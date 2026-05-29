using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supermarket.Api.Controllers;
using Supermarket.Api.Filters;
using Supermarket.Application.AuditLogs.Services;
using Supermarket.Contracts.AuditLogs;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;
using Supermarket.Infrastructure.Repositories;
using Xunit;

namespace Supermarket.IntegrationTests.AuditLogs
{
    public class AuditLogsEndpointTests
    {
        private SupermarketDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SupermarketDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new SupermarketDbContext(options);
        }

        [Fact]
        public async Task GetAuditLogs_Endpoint_Requires_ActiveSession_And_SettingsPermission()
        {
            // Verify attributes on the Controller class using reflection
            var type = typeof(AuditLogsController);
            var activeSessionAttr = type.GetCustomAttribute<RequireActiveSessionAttribute>();
            var screenPermissionAttr = type.GetCustomAttribute<RequireScreenPermissionAttribute>();

            Assert.NotNull(activeSessionAttr);
            Assert.NotNull(screenPermissionAttr);

            var screenKeyField = typeof(RequireScreenPermissionAttribute).GetField("_screenKey", BindingFlags.NonPublic | BindingFlags.Instance);
            var screenKeyValue = screenKeyField?.GetValue(screenPermissionAttr) as string;
            Assert.Equal("AuditLogs", screenKeyValue);

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GetStatus_Endpoint_Returns_Counts()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            
            var now = DateTime.UtcNow;
            db.AuditLogs.Add(new AuditLog
            {
                Id = 1L,
                EmployeeId = 1L,
                Action = "CREATE",
                EntityType = "Product",
                BeforeJson = "",
                AfterJson = "{\"Name\": \"Apple\"}", // approximate large json since not null/empty
                CreatedAt = now.AddDays(-2)
            });

            db.AuditLogs.Add(new AuditLog
            {
                Id = 2L,
                EmployeeId = 1L,
                Action = "UPDATE",
                EntityType = "Product",
                BeforeJson = "{}",
                AfterJson = "{}",
                CreatedAt = now
            });

            await db.SaveChangesAsync();

            var repository = new AuditLogRepository(db);
            var queryService = new AuditLogQueryService(repository);
            var controller = new AuditLogsController(queryService);

            // Act
            var result = await controller.GetStatus();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuditLogStatusResponse>(okResult.Value);

            Assert.Equal(2, response.TotalCount);
            Assert.Equal(now.AddDays(-2).Date, response.OldestCreatedAt?.Date);
            Assert.Equal(now.Date, response.NewestCreatedAt?.Date);
            Assert.Equal(2, response.ApproximateLargeJsonCount); // both have non-empty jsons
            Assert.Equal(180, response.RecommendedRetentionDays);
            Assert.False(response.CleanupEnabled);
        }

        [Fact]
        public async Task GetStatus_Endpoint_WithEmptyTable_Returns_ZeroCountAndNullDates()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var repository = new AuditLogRepository(db);
            var queryService = new AuditLogQueryService(repository);
            var controller = new AuditLogsController(queryService);

            // Act
            var result = await controller.GetStatus();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuditLogStatusResponse>(okResult.Value);

            Assert.Equal(0, response.TotalCount);
            Assert.Null(response.OldestCreatedAt);
            Assert.Null(response.NewestCreatedAt);
            Assert.Equal(0, response.ApproximateLargeJsonCount);
            Assert.Equal(180, response.RecommendedRetentionDays);
            Assert.False(response.CleanupEnabled);
        }

        [Fact]
        public async Task GetAuditLogById_Endpoint_Returns_NotFound_IfMissing()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var repository = new AuditLogRepository(db);
            var queryService = new AuditLogQueryService(repository);
            var controller = new AuditLogsController(queryService);

            // Act
            var result = await controller.GetById(999L);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
