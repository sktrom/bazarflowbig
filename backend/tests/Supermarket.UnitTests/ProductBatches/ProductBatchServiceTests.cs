using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.ProductBatches.Interfaces;
using Supermarket.Application.ProductBatches.Services;
using Supermarket.Contracts.ProductBatches;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.ProductBatches
{
    public class ProductBatchServiceTests
    {
        private readonly Mock<IBatchManagementRepository> _repoMock;
        private readonly Mock<ISessionContext> _sessionMock;
        private readonly ProductBatchService _service;

        public ProductBatchServiceTests()
        {
            _repoMock = new Mock<IBatchManagementRepository>();
            _sessionMock = new Mock<ISessionContext>();
            _service = new ProductBatchService(_repoMock.Object, _sessionMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenProductNotFound()
        {
            var request = new CreateBatchRequest 
            { 
                QuantityReceived = 10, 
                QuantityAvailable = 10 
            };
            
            _repoMock.Setup(r => r.ProductExistsAsync(99)).ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(99, request));
            Assert.Equal("PRODUCT_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenNoActiveSession()
        {
            var request = new CreateBatchRequest 
            { 
                QuantityReceived = 10, 
                QuantityAvailable = 10 
            };
            
            _repoMock.Setup(r => r.ProductExistsAsync(1)).ReturnsAsync(true);
            _sessionMock.Setup(s => s.EmployeeId).Returns(0); // Invalid session employee

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(1, request));
            Assert.Equal("NO_ACTIVE_SESSION", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_AndAssignSessionEmployeeId_WhenValid()
        {
            var request = new CreateBatchRequest 
            { 
                QuantityReceived = 10, 
                QuantityAvailable = 10 
            };
            
            _repoMock.Setup(r => r.ProductExistsAsync(1)).ReturnsAsync(true);
            _sessionMock.Setup(s => s.EmployeeId).Returns(42);
            
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<ProductBatch>())).ReturnsAsync((ProductBatch b) => 
            {
                b.Id = 100;
                return b;
            });

            var result = await _service.CreateAsync(1, request);

            Assert.Equal(1, result.ProductId);
            Assert.Equal(42, result.EnteredByEmployeeId);
            _repoMock.Verify(r => r.CreateAsync(It.Is<ProductBatch>(b => b.EnteredByEmployeeId == 42 && b.ProductId == 1)), Times.Once);
        }
    }
}
