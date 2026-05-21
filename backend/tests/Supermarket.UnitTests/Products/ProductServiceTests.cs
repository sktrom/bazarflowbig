using System;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Products.Services;
using Supermarket.Contracts.Products;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Products
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductManagementRepository> _repoMock;
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _repoMock = new Mock<IProductManagementRepository>();
            _auditLogMock = new Mock<IAuditLogService>();
            _service = new ProductService(_repoMock.Object, _auditLogMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_WhenBarcodeIsUniqueAndCategoryExists()
        {
            var request = new CreateProductRequest 
            { 
                Name = "Test Product", 
                Barcode = "12345", 
                CategoryId = 1,
                BaseUnit = "pcs"
            };

            _repoMock.Setup(r => r.GetByBarcodeAsync("12345")).ReturnsAsync((Product?)null);
            _repoMock.Setup(r => r.CategoryExistsAsync(1)).ReturnsAsync(true);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>())).ReturnsAsync((Product p) => 
            {
                p.Id = 1;
                return p;
            });

            var result = await _service.CreateAsync(request);

            Assert.Equal("12345", result.Barcode);
            Assert.True(result.IsActive);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Product>(p => p.Barcode == "12345")), Times.Once);
            VerifyAudit("PRODUCT_CREATE", "1", "Test Product");
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenBarcodeExists()
        {
            var request = new CreateProductRequest { Name = "Test", Barcode = "12345", CategoryId = 1, BaseUnit = "pcs" };
            
            _repoMock.Setup(r => r.CategoryExistsAsync(1)).ReturnsAsync(true);
            _repoMock.Setup(r => r.GetByBarcodeAsync("12345")).ReturnsAsync(new Product());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
            Assert.Equal("BARCODE_ALREADY_EXISTS", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenCategoryDoesNotExist()
        {
            var request = new CreateProductRequest { Name = "Test", Barcode = "12345", CategoryId = 99, BaseUnit = "pcs" };
            
            _repoMock.Setup(r => r.CategoryExistsAsync(99)).ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
            Assert.Equal("CATEGORY_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldSucceed_WhenDataIsValid()
        {
            var request = new UpdateProductRequest { Name = "Updated", Barcode = "54321", CategoryId = 2, BaseUnit = "box", IsActive = false };
            var existingProduct = new Product { Id = 1, Name = "Old", Barcode = "12345", CategoryId = 1, IsActive = true };
            
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);
            _repoMock.Setup(r => r.GetByBarcodeAsync("54321")).ReturnsAsync((Product?)null);
            _repoMock.Setup(r => r.CategoryExistsAsync(2)).ReturnsAsync(true);

            var result = await _service.UpdateAsync(1, request);

            Assert.Equal("54321", result.Barcode);
            Assert.Equal(2, result.CategoryId);
            Assert.False(result.IsActive);
            _repoMock.Verify(r => r.UpdateAsync(existingProduct), Times.Once);
            VerifyAudit("PRODUCT_UPDATE", "1", "Updated");
        }

        [Fact]
        public async Task UpdateAsync_ShouldFail_WhenBarcodeConflictsWithOtherProduct()
        {
            var request = new UpdateProductRequest { Name = "Test", Barcode = "CONFLICT", CategoryId = 1, BaseUnit = "pcs" };
            var existingProduct = new Product { Id = 1, Barcode = "12345" };
            var conflictingProduct = new Product { Id = 2, Barcode = "CONFLICT" };
            
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);
            _repoMock.Setup(r => r.GetByBarcodeAsync("CONFLICT")).ReturnsAsync(conflictingProduct);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(1, request));
            Assert.Equal("BARCODE_ALREADY_EXISTS", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_ShouldPhysicalDelete_WhenNotInUse()
        {
            var existingProduct = new Product { Id = 1, Name = "Unused Product" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);
            _repoMock.Setup(r => r.HasRelatedRecordsAsync(1)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(1);

            Assert.Equal("DELETED", result.Action);
            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
            VerifyAudit("PRODUCT_DELETE", "1", "Unused Product");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDisable_WhenHasBatchesOrInvoices()
        {
            var existingProduct = new Product { Id = 1, Name = "Used Product", IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);
            _repoMock.Setup(r => r.HasRelatedRecordsAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(1);

            Assert.Equal("DISABLED", result.Action);
            Assert.False(existingProduct.IsActive);
            _repoMock.Verify(r => r.UpdateAsync(existingProduct), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            VerifyAudit("PRODUCT_DEACTIVATE", "1", "Used Product");
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_WhenAuditThrows()
        {
            var request = new CreateProductRequest { Name = "Test Product", Barcode = "12345", CategoryId = 1, BaseUnit = "pcs" };

            _repoMock.Setup(r => r.GetByBarcodeAsync("12345")).ReturnsAsync((Product?)null);
            _repoMock.Setup(r => r.CategoryExistsAsync(1)).ReturnsAsync(true);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>())).ReturnsAsync((Product p) =>
            {
                p.Id = 1;
                return p;
            });
            _auditLogMock
                .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<object?>()))
                .ThrowsAsync(new Exception("audit failed"));

            var result = await _service.CreateAsync(request);

            Assert.Equal(1, result.Id);
        }

        private void VerifyAudit(string action, string entityId, string? entityDisplayName)
        {
            _auditLogMock.Verify(a => a.RecordAsync(
                action,
                "Product",
                entityId,
                entityDisplayName,
                null,
                null,
                It.IsAny<object>()), Times.Once);
        }
    }
}
