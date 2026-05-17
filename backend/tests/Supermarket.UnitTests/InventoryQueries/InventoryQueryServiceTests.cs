using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Application.InventoryQueries.Services;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.InventoryQueries
{
    public class InventoryQueryServiceTests
    {
        private readonly Mock<IInventoryQueryRepository> _repoMock = new();
        private readonly Mock<IAppSettingsRepository> _settingsMock = new();

        private InventoryQueryService CreateService()
        {
            _settingsMock.Setup(s => s.GetRequiredDecimalAsync("stock_alert_threshold")).ReturnsAsync(5);
            _settingsMock.Setup(s => s.GetRequiredDecimalAsync("expiry_alert_days")).ReturnsAsync(30);
            return new InventoryQueryService(_repoMock.Object, _settingsMock.Object);
        }

        [Fact]
        public async Task GetInventoryDetails_ShouldThrow_WhenProductNotFound()
        {
            _repoMock.Setup(r => r.GetProductByIdAsync(99)).ReturnsAsync((Product?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetInventoryDetailsAsync(99));

            Assert.Equal("PRODUCT_NOT_FOUND", ex.Message);
        }

        [Fact]
        public async Task GetInventoryList_ShouldComputeStockStatus_FromSettings()
        {
            var items = new List<(Product, decimal, int, DateTime?)>
            {
                (new Product { Id = 1, Name = "P1" }, 0, 0, null), // OutOfStock
                (new Product { Id = 2, Name = "P2" }, 3, 1, null), // LowStock (<= 5)
                (new Product { Id = 3, Name = "P3" }, 10, 2, null) // InStock (> 5)
            };

            _repoMock.Setup(r => r.GetInventoryPaginatedAsync(null, null, null, null, null, 1, 20))
                .ReturnsAsync((items, 3));

            var service = CreateService();
            var result = await service.GetInventoryListAsync(null, null, null, null, null, 1, 20);

            Assert.Equal("OutOfStock", result.Items[0].StockStatus);
            Assert.Equal("LowStock", result.Items[1].StockStatus);
            Assert.Equal("InStock", result.Items[2].StockStatus);
        }

        [Fact]
        public async Task GetInventoryDetails_ShouldComputeExpiryStatus_FromSettings()
        {
            var product = new Product { Id = 1, Name = "P1", HasExpiry = true };
            var now = DateTime.UtcNow;

            var batches = new List<ProductBatch>
            {
                new ProductBatch { Id = 10, QuantityAvailable = 10, ExpiryDate = now.AddDays(-1) }, // Expired
                new ProductBatch { Id = 11, QuantityAvailable = 10, ExpiryDate = now.AddDays(15) }, // ExpiringSoon (<= 30)
                new ProductBatch { Id = 12, QuantityAvailable = 10, ExpiryDate = now.AddDays(40) }  // Fresh (> 30)
            };

            _repoMock.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
            _repoMock.Setup(r => r.GetProductBatchesAsync(1)).ReturnsAsync(batches);

            var service = CreateService();
            var result = await service.GetInventoryDetailsAsync(1);

            Assert.Equal("Expired", result.Batches[0].ExpiryStatus);
            Assert.Equal("ExpiringSoon", result.Batches[1].ExpiryStatus);
            Assert.Equal("Fresh", result.Batches[2].ExpiryStatus);

            // Details-level nearest expiry is MIN of ExpiryDate (which is -1 day = Expired)
            Assert.Equal("Expired", result.ExpiryStatus);
        }
    }
}
