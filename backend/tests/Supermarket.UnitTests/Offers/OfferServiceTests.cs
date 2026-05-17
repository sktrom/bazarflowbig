using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Offers.Services;
using Supermarket.Contracts.Offers;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.Offers
{
    public class OfferServiceTests
    {
        private readonly Mock<IOfferManagementRepository> _repoMock;
        private readonly OfferService _service;

        public OfferServiceTests()
        {
            _repoMock = new Mock<IOfferManagementRepository>();
            _service = new OfferService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenProductNotFound()
        {
            var request = new CreateOfferRequest { ProductId = 99, DiscountType = "Percent", DiscountValue = 10 };
            _repoMock.Setup(r => r.ProductExistsAsync(99)).ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
            Assert.Equal("PRODUCT_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenDiscountTypeIsInvalid()
        {
            var request = new CreateOfferRequest { ProductId = 1, DiscountType = "InvalidType", DiscountValue = 10 };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
            Assert.Equal("INVALID_DISCOUNT_TYPE", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_WhenValidEntity()
        {
            var request = new CreateOfferRequest { ProductId = 1, DiscountType = "Percent", DiscountValue = 20 };
            
            _repoMock.Setup(r => r.ProductExistsAsync(1)).ReturnsAsync(true);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Offer>())).ReturnsAsync((Offer o) => 
            {
                o.Id = 100;
                return o;
            });

            var result = await _service.CreateAsync(request);

            Assert.Equal("Percent", result.DiscountType);
            Assert.True(result.IsActive);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Offer>(o => o.ProductId == 1 && o.DiscountValue == 20)), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_ShouldAlwaysDeactivateOffer_RegardlessOfUsageOrLegacy()
        {
            var existingOffer = new Offer { Id = 1, IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOffer);

            var result = await _service.CancelAsync(1);

            Assert.True(result.Success);
            Assert.False(existingOffer.IsActive);
            _repoMock.Verify(r => r.UpdateAsync(existingOffer), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldFail_WhenAdoptionMarkerIsMissing()
        {
            var existingOffer = new Offer { Id = 1, CreatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOffer);
            _repoMock.Setup(r => r.GetAdoptionMarkerUtcAsync()).ReturnsAsync((DateTime?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
            Assert.Equal("MISSING_ADOPTION_MARKER_CONFIGURATION", exception.Message);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldFail_ForLegacyOffers_BeforeAdoptionMarker()
        {
            var markerDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var legacyOffer = new Offer { Id = 1, CreatedAt = markerDate.AddDays(-1) }; // Legacy

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(legacyOffer);
            _repoMock.Setup(r => r.GetAdoptionMarkerUtcAsync()).ReturnsAsync(markerDate);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
            Assert.Equal("CANNOT_DELETE_LEGACY_OFFER", exception.Message);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldFail_WhenProvableButUsedInInvoiceLines()
        {
            var markerDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var provableOffer = new Offer { Id = 1, CreatedAt = markerDate.AddDays(1) }; // Provable

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(provableOffer);
            _repoMock.Setup(r => r.GetAdoptionMarkerUtcAsync()).ReturnsAsync(markerDate);
            _repoMock.Setup(r => r.HasInvoiceLinesAsync(1)).ReturnsAsync(true); // Used

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
            Assert.Equal("CANNOT_DELETE_USED_OFFER", exception.Message);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldSucceed_WhenProvableAndUnused()
        {
            var markerDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var provableOffer = new Offer { Id = 1, CreatedAt = markerDate.AddDays(1) }; // Provable

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(provableOffer);
            _repoMock.Setup(r => r.GetAdoptionMarkerUtcAsync()).ReturnsAsync(markerDate);
            _repoMock.Setup(r => r.HasInvoiceLinesAsync(1)).ReturnsAsync(false); // Unused

            var result = await _service.DeleteAsync(1);

            Assert.True(result.Success);
            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
