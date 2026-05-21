using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
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
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly OfferService _service;

        public OfferServiceTests()
        {
            _repoMock = new Mock<IOfferManagementRepository>();
            _auditLogMock = new Mock<IAuditLogService>();
            _service = new OfferService(_repoMock.Object, _auditLogMock.Object);
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
            VerifyAudit("OFFER_CREATE", "100");
        }

        [Fact]
        public async Task UpdateAsync_ShouldSucceed_WhenValidEntity()
        {
            var existingOffer = new Offer { Id = 100, ProductId = 1, DiscountType = OfferDiscountType.Percent, DiscountValue = 10, IsActive = true };
            var request = new UpdateOfferRequest { ProductId = 2, DiscountType = "Amount", DiscountValue = 5 };

            _repoMock.Setup(r => r.ProductExistsAsync(2)).ReturnsAsync(true);
            _repoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(existingOffer);

            var result = await _service.UpdateAsync(100, request);

            Assert.Equal(2, result.ProductId);
            Assert.Equal("Amount", result.DiscountType);
            Assert.Equal(5, result.DiscountValue);
            _repoMock.Verify(r => r.UpdateAsync(existingOffer), Times.Once);
            VerifyAudit("OFFER_UPDATE", "100");
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
            VerifyAudit("OFFER_CANCEL", "1");
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
            VerifyAudit("OFFER_DELETE", "1");
        }

        // --- ProductsLookup tests ---

        [Fact]
        public async Task ProductsLookupAsync_ShouldReturnOnlyActiveProducts()
        {
            var activeProducts = new List<Product>
            {
                new Product { Id = 1, Name = "كولا", Barcode = "111", IsActive = true, PriceUsd = 1.5m },
                new Product { Id = 2, Name = "ماء",  Barcode = "222", IsActive = true, PriceUsd = 0.5m }
            };

            _repoMock
                .Setup(r => r.ProductsLookupAsync(null, 20))
                .ReturnsAsync(activeProducts);

            var result = await _service.ProductsLookupAsync(null);

            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, i => Assert.True(i.ProductId > 0));
            _repoMock.Verify(r => r.ProductsLookupAsync(null, 20), Times.Once);
        }

        [Fact]
        public async Task ProductsLookupAsync_ShouldSearchByBarcode()
        {
            var matchedProduct = new Product { Id = 3, Name = "عصير", Barcode = "555", IsActive = true, PriceUsd = 2m };

            _repoMock
                .Setup(r => r.ProductsLookupAsync("555", 20))
                .ReturnsAsync(new List<Product> { matchedProduct });

            var result = await _service.ProductsLookupAsync("555");

            Assert.Single(result.Items);
            Assert.Equal("555", result.Items[0].Barcode);
        }

        [Fact]
        public async Task ProductsLookupAsync_ShouldNotExceedLimit20()
        {
            // Repository is responsible for enforcing the limit;
            // service always passes 20 — verify that contract.
            _repoMock
                .Setup(r => r.ProductsLookupAsync(It.IsAny<string?>(), 20))
                .ReturnsAsync(new List<Product>());

            await _service.ProductsLookupAsync("anything");

            _repoMock.Verify(r => r.ProductsLookupAsync(It.IsAny<string?>(), 20), Times.Once);
        }

        private void VerifyAudit(string action, string entityId)
        {
            _auditLogMock.Verify(a => a.RecordAsync(
                action,
                "Offer",
                entityId,
                null,
                null,
                null,
                It.IsAny<object>()), Times.Once);
        }
    }
}
