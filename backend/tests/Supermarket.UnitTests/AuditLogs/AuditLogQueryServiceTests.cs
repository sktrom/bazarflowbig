using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.AuditLogs.Services;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.AuditLogs
{
    public class AuditLogQueryServiceTests
    {
        private readonly Mock<IAuditLogRepository> _repoMock = new();

        [Fact]
        public async Task GetPagedAsync_ShouldApplyFilterParameters_ToRepository()
        {
            var dateFrom = DateTime.UtcNow.AddDays(-1);
            var dateTo = DateTime.UtcNow;
            
            _repoMock.Setup(r => r.GetPagedAsync(123L, "CREATE", "Product", dateFrom, dateTo, 1, 50))
                .ReturnsAsync((new List<AuditLog>(), 0));

            var service = new AuditLogQueryService(_repoMock.Object);
            var result = await service.GetPagedAsync(123L, "CREATE", "Product", dateFrom, dateTo, 1, 50);

            Assert.Equal(0, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(50, result.PageSize);
            _repoMock.Verify(r => r.GetPagedAsync(123L, "CREATE", "Product", dateFrom, dateTo, 1, 50), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldEnforcePageSizeLimits()
        {
            _repoMock.Setup(r => r.GetPagedAsync(null, null, null, null, null, 1, 200))
                .ReturnsAsync((new List<AuditLog>(), 0));

            var service = new AuditLogQueryService(_repoMock.Object);
            
            // Testing too large page size
            var resultLarge = await service.GetPagedAsync(null, null, null, null, null, 1, 1000);
            Assert.Equal(200, resultLarge.PageSize);

            // Testing negative values
            _repoMock.Setup(r => r.GetPagedAsync(null, null, null, null, null, 1, 50))
                .ReturnsAsync((new List<AuditLog>(), 0));
            var resultNegative = await service.GetPagedAsync(null, null, null, null, null, -5, -10);
            Assert.Equal(1, resultNegative.Page);
            Assert.Equal(50, resultNegative.PageSize);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var log = new AuditLog
            {
                Id = 456L,
                EmployeeId = 1L,
                Employee = new Employee { FullName = "Ali" },
                Action = "UPDATE",
                EntityType = "Category",
                BeforeJson = "{}",
                AfterJson = "{}",
                MetadataJson = "{}",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla"
            };

            _repoMock.Setup(r => r.GetByIdAsync(456L)).ReturnsAsync(log);

            var service = new AuditLogQueryService(_repoMock.Object);
            var result = await service.GetByIdAsync(456L);

            Assert.NotNull(result);
            Assert.Equal(456L, result!.Id);
            Assert.Equal("Ali", result.EmployeeName);
            Assert.Equal("UPDATE", result.Action);
            Assert.Equal("127.0.0.1", result.IpAddress);
            Assert.Equal("Mozilla", result.UserAgent);
            Assert.True(result.HasBefore);
            Assert.True(result.HasAfter);
            Assert.True(result.HasMetadata);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999L)).ReturnsAsync((AuditLog?)null);

            var service = new AuditLogQueryService(_repoMock.Object);
            var result = await service.GetByIdAsync(999L);

            Assert.Null(result);
        }
    }
}
