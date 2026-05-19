using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Application.InventoryQueries.Services;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.InventoryQueries
{
    public class ActionCenterServiceTests
    {
        private readonly Mock<IInventoryQueryRepository> _mockRepo;
        private readonly Mock<IAppSettingsRepository> _mockSettings;
        private readonly ActionCenterService _service;

        public ActionCenterServiceTests()
        {
            _mockRepo = new Mock<IInventoryQueryRepository>();
            _mockSettings = new Mock<IAppSettingsRepository>();

            _mockSettings.Setup(s => s.GetRequiredDecimalAsync("stock_alert_threshold")).ReturnsAsync(10m);
            _mockSettings.Setup(s => s.GetRequiredDecimalAsync("expiry_alert_days")).ReturnsAsync(30m);

            _service = new ActionCenterService(_mockRepo.Object, _mockSettings.Object);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_ShouldIdentifyOutOfStock()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Test Product", Barcode = "123", IsActive = true };
            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (product, 0m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());

            // Act
            var result = await _service.GetActionCenterSummaryAsync();

            // Assert
            Assert.Single(result.OutOfStock);
            Assert.Equal(1, result.Summary.OutOfStockCount);
            Assert.Contains(result.TopUrgentActions, a => a.Type == "OUT_OF_STOCK" && a.Severity == "HIGH");
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_ShouldIdentifyExpiredAndExpiringSoon()
        {
            // Arrange
            var product1 = new Product { Id = 1, Name = "P1", IsActive = true };
            var product2 = new Product { Id = 2, Name = "P2", IsActive = true };
            
            var today = DateTime.UtcNow.Date;
            
            var batchExpired = new ProductBatch { Id = 10, ProductId = 1, QuantityAvailable = 5, ExpiryDate = today.AddDays(-1) };
            var batchExpiringSoon = new ProductBatch { Id = 20, ProductId = 2, QuantityAvailable = 10, ExpiryDate = today.AddDays(15) };

            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> 
                { 
                    (product1, 5m), 
                    (product2, 10m) 
                });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync())
                .ReturnsAsync(new List<(ProductBatch, Product)> 
                { 
                    (batchExpired, product1), 
                    (batchExpiringSoon, product2) 
                });
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());

            // Act
            var result = await _service.GetActionCenterSummaryAsync();

            // Assert
            Assert.Single(result.Expired);
            Assert.Single(result.ExpiringSoon);
            
            // Should also suggest offer for expiring soon
            Assert.Single(result.OfferCandidates);
            Assert.Equal(2, result.OfferCandidates.First().ProductId);
            
            Assert.Contains(result.TopUrgentActions, a => a.Type == "EXPIRED" && a.Severity == "HIGH");
            Assert.Contains(result.TopUrgentActions, a => a.Type == "EXPIRING_SOON" && a.Severity == "MEDIUM");
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_TopUrgentActionsShouldNotExceed10()
        {
            // Arrange
            var products = new List<(Product, decimal)>();
            for (int i = 1; i <= 15; i++)
            {
                products.Add((new Product { Id = i, Name = $"P{i}", IsActive = true }, 0m)); // OutOfStock -> HIGH
            }

            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync()).ReturnsAsync(products);
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());

            // Act
            var result = await _service.GetActionCenterSummaryAsync();

            // Assert
            Assert.Equal(15, result.OutOfStock.Count);
            Assert.Equal(10, result.TopUrgentActions.Count);
            Assert.All(result.TopUrgentActions, a => Assert.Equal("HIGH", a.Severity));
        }
    }
}
