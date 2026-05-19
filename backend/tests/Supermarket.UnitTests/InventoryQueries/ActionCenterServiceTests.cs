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
            _mockRepo.Setup(r => r.GetSoldQuantitiesLast30DaysAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<long, decimal>());

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

        [Fact]
        public async Task GetActionCenterSummaryAsync_ShouldIdentifyInactiveWithStock()
        {
            // Arrange
            var activeProduct   = new Product { Id = 1, Name = "Active P",   IsActive = true  };
            var inactiveProduct = new Product { Id = 2, Name = "Inactive P", IsActive = false };

            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)>
                {
                    (activeProduct,   15m), // active + stock > threshold -> InStock, not listed
                    (inactiveProduct, 20m)  // inactive + stock > 0 -> InactiveWithStock
                });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());

            // Act
            var result = await _service.GetActionCenterSummaryAsync();

            // Assert
            Assert.Single(result.InactiveWithStock);
            Assert.Equal(2, result.InactiveWithStock.First().ProductId);
            Assert.Equal(1, result.Summary.InactiveWithStockCount);
            Assert.Empty(result.OutOfStock);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_ShouldIdentifyOfferCandidatesFromSlowMoving()
        {
            // Arrange — product with stock but zero sales last 30 days (slow moving)
            var slowProduct = new Product { Id = 10, Name = "Slow P", Barcode = "SLOW", IsActive = true };

            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (slowProduct, 50m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            // Slow moving: product Id 10 returned by repository
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync())
                .ReturnsAsync(new List<long> { 10L });

            // Act
            var result = await _service.GetActionCenterSummaryAsync();

            // Assert
            Assert.Single(result.OfferCandidates);
            Assert.Equal(10, result.OfferCandidates.First().ProductId);
            Assert.Equal(1, result.Summary.OfferCandidatesCount);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_TopUrgentActionsShouldBeOrderedHighFirst()
        {
            // Arrange — mix of HIGH (expired) and MEDIUM (expiring soon) items
            var pExpired      = new Product { Id = 1, Name = "Exp",  IsActive = true };
            var pExpiringSoon = new Product { Id = 2, Name = "Soon", IsActive = true };

            var today = DateTime.UtcNow.Date;
            var batchExpired      = new ProductBatch { Id = 1, ProductId = 1, QuantityAvailable = 5,  ExpiryDate = today.AddDays(-1) };
            var batchExpiringSoon = new ProductBatch { Id = 2, ProductId = 2, QuantityAvailable = 10, ExpiryDate = today.AddDays(10) };

            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (pExpired, 5m), (pExpiringSoon, 10m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync())
                .ReturnsAsync(new List<(ProductBatch, Product)> { (batchExpired, pExpired), (batchExpiringSoon, pExpiringSoon) });
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());

            // Act
            var result = await _service.GetActionCenterSummaryAsync();

            // Assert — HIGH must come before MEDIUM
            Assert.True(result.TopUrgentActions.Count >= 2);
            Assert.Equal("HIGH",   result.TopUrgentActions[0].Severity);
            Assert.Equal("MEDIUM", result.TopUrgentActions[1].Severity);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_RestockZeroStockWithSalesShouldBeBuyNow()
        {
            var product = new Product { Id = 1, Name = "Fast P", Barcode = "FAST", IsActive = true };
            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (product, 0m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());
            _mockRepo.Setup(r => r.GetSoldQuantitiesLast30DaysAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<long, decimal> { [1] = 12m });

            var result = await _service.GetActionCenterSummaryAsync();

            var suggestion = Assert.Single(result.RestockSuggestions);
            Assert.Equal("BuyNow", suggestion.RecommendationType);
            Assert.Equal("High", suggestion.Confidence);
            Assert.Equal(6m, suggestion.SuggestedQty);
            Assert.Contains(result.TopUrgentActions, a => a.Type == "RESTOCK_BUY_NOW" && a.Severity == "HIGH");
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_RestockDaysRemainingAtMost3ShouldBeBuyNow()
        {
            var product = new Product { Id = 2, Name = "Urgent P", Barcode = "URG", IsActive = true };
            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (product, 2m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());
            _mockRepo.Setup(r => r.GetSoldQuantitiesLast30DaysAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<long, decimal> { [2] = 30m });

            var result = await _service.GetActionCenterSummaryAsync();

            var suggestion = Assert.Single(result.RestockSuggestions);
            Assert.Equal("BuyNow", suggestion.RecommendationType);
            Assert.Equal(2m, suggestion.DaysRemaining);
            Assert.Equal(12m, suggestion.SuggestedQty);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_RestockDaysRemainingAtMost7ShouldBeWatch()
        {
            var product = new Product { Id = 3, Name = "Watch P", Barcode = "WCH", IsActive = true };
            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (product, 5m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long>());
            _mockRepo.Setup(r => r.GetSoldQuantitiesLast30DaysAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<long, decimal> { [3] = 30m });

            var result = await _service.GetActionCenterSummaryAsync();

            var suggestion = Assert.Single(result.RestockSuggestions);
            Assert.Equal("Watch", suggestion.RecommendationType);
            Assert.Equal(5m, suggestion.DaysRemaining);
            Assert.Equal(9m, suggestion.SuggestedQty);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_RestockStockWithZeroSalesShouldBeSlowMoving()
        {
            var product = new Product { Id = 4, Name = "Slow P", Barcode = "SLOW", IsActive = true };
            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (product, 20m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long> { 4L });

            var result = await _service.GetActionCenterSummaryAsync();

            var suggestion = Assert.Single(result.RestockSuggestions);
            Assert.Equal("SlowMoving", suggestion.RecommendationType);
            Assert.Equal("Low", suggestion.Confidence);
            Assert.Null(suggestion.DaysRemaining);
            Assert.Equal(0m, suggestion.SuggestedQty);
        }

        [Fact]
        public async Task GetActionCenterSummaryAsync_RestockShouldCalculateSuggestedQtyAndConfidence()
        {
            var high = new Product { Id = 5, Name = "High P", Barcode = "HIGH", IsActive = true };
            var medium = new Product { Id = 6, Name = "Medium P", Barcode = "MED", IsActive = true };
            var low = new Product { Id = 7, Name = "Low P", Barcode = "LOW", IsActive = true };
            _mockRepo.Setup(r => r.GetProductsWithStockLevelsAsync())
                .ReturnsAsync(new List<(Product, decimal)> { (high, 2m), (medium, 1m), (low, 10m) });
            _mockRepo.Setup(r => r.GetBatchesWithExpiryAsync()).ReturnsAsync(new List<(ProductBatch, Product)>());
            _mockRepo.Setup(r => r.GetProductsWithZeroSalesLast30DaysAsync()).ReturnsAsync(new List<long> { 7L });
            _mockRepo.Setup(r => r.GetSoldQuantitiesLast30DaysAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<long, decimal> { [5] = 15m, [6] = 6m });

            var result = await _service.GetActionCenterSummaryAsync();

            Assert.Equal(3, result.RestockSuggestions.Count);
            Assert.Equal("High", result.RestockSuggestions.Single(x => x.ProductId == 5).Confidence);
            Assert.Equal(5m, result.RestockSuggestions.Single(x => x.ProductId == 5).SuggestedQty);
            Assert.Equal("Medium", result.RestockSuggestions.Single(x => x.ProductId == 6).Confidence);
            Assert.Equal(2m, result.RestockSuggestions.Single(x => x.ProductId == 6).SuggestedQty);
            Assert.Equal("Low", result.RestockSuggestions.Single(x => x.ProductId == 7).Confidence);
        }
    }
}
