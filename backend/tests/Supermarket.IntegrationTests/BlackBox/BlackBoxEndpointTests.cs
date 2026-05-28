using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supermarket.Api.Controllers;
using Supermarket.Api.Filters;
using Supermarket.Application.BlackBox.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.BlackBox;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;
using Supermarket.Infrastructure.Repositories;
using Xunit;

namespace Supermarket.IntegrationTests.BlackBox
{
    public class BlackBoxEndpointTests
    {
        [Fact]
        public void BlackBoxEndpoints_HaveExpectedAuthorizationAttributes()
        {
            var controllerType = typeof(BlackBoxController);
            Assert.NotNull(controllerType.GetCustomAttribute<RequireActiveSessionAttribute>());

            var createMethod = controllerType.GetMethod(nameof(BlackBoxController.Create));
            Assert.NotNull(createMethod);
            Assert.Null(createMethod!.GetCustomAttribute<RequireScreenPermissionAttribute>());

            var getPagedMethod = controllerType.GetMethod(nameof(BlackBoxController.GetPaged));
            var getByIdMethod = controllerType.GetMethod(nameof(BlackBoxController.GetById));

            AssertScreenPermission(getPagedMethod, "BlackBox");
            AssertScreenPermission(getByIdMethod, "BlackBox");
        }

        [Fact]
        public async Task PostEvent_CreatesRow_WithServerSideIdentity()
        {
            await using var db = CreateInMemoryDbContext();
            var controller = CreateController(db, new TestSessionContext(11, 22, "POS-2", true));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            controller.ControllerContext.HttpContext.Request.Headers["User-Agent"] = "integration-test-agent";

            var result = await controller.Create(new CreateBlackBoxEventRequest
            {
                Route = "/cashier",
                PageName = "Cashier",
                ActionType = "BUTTON_CLICK",
                ElementKey = "complete-sale",
                Result = "SUCCESS",
                Metadata = new Dictionary<string, object?> { ["amount"] = 12.5m }
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CreateBlackBoxEventResponse>(okResult.Value);
            Assert.True(response.Success);

            var saved = await db.BlackBoxEvents.SingleAsync();
            Assert.Equal(11, saved.EmployeeId);
            Assert.Equal(22, saved.SessionId);
            Assert.Equal("POS-2", saved.DeviceCode);
            Assert.Equal("127.0.0.1", saved.IpAddress);
            Assert.Equal("integration-test-agent", saved.UserAgent);
            Assert.Contains("amount", saved.MetadataJson);
        }

        [Fact]
        public async Task GetPaged_ReturnsListWithoutMetadataJson()
        {
            await using var db = CreateInMemoryDbContext();
            db.BlackBoxEvents.Add(new BlackBoxEvent
            {
                ActionType = "OPEN_PAGE",
                Result = "SUCCESS",
                PageName = "Reports",
                MetadataJson = "{\"secret\":false}",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db, new TestSessionContext(1, 1, "POS-1", true));

            var result = await controller.GetPaged(new BlackBoxEventQuery());

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BlackBoxEventListResponse>(okResult.Value);
            var item = Assert.Single(response.Items);
            Assert.True(item.HasMetadata);
            Assert.DoesNotContain(typeof(BlackBoxEventListItem).GetProperties(), x => x.Name == "MetadataJson");
        }

        [Fact]
        public async Task GetById_ReturnsDetailWithSanitizedMetadata()
        {
            await using var db = CreateInMemoryDbContext();
            db.BlackBoxEvents.Add(new BlackBoxEvent
            {
                Id = 1,
                ActionType = "OPEN_PAGE",
                Result = "SUCCESS",
                MetadataJson = "{\"safe\":true}",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db, new TestSessionContext(1, 1, "POS-1", true));

            var result = await controller.GetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BlackBoxEventDetailResponse>(okResult.Value);
            Assert.Equal("{\"safe\":true}", response.MetadataJson);
        }

        [Fact]
        public async Task GetPaged_FiltersByActionTypeResultAndDate()
        {
            await using var db = CreateInMemoryDbContext();
            var now = DateTime.UtcNow;
            db.BlackBoxEvents.AddRange(
                new BlackBoxEvent
                {
                    ActionType = "SAVE",
                    Result = "SUCCESS",
                    PageName = "Products",
                    CreatedAtUtc = now
                },
                new BlackBoxEvent
                {
                    ActionType = "SAVE",
                    Result = "FAILED",
                    PageName = "Products",
                    CreatedAtUtc = now
                },
                new BlackBoxEvent
                {
                    ActionType = "OPEN_PAGE",
                    Result = "SUCCESS",
                    PageName = "Products",
                    CreatedAtUtc = now.AddDays(-10)
                });
            await db.SaveChangesAsync();

            var controller = CreateController(db, new TestSessionContext(1, 1, "POS-1", true));

            var result = await controller.GetPaged(new BlackBoxEventQuery
            {
                ActionType = "SAVE",
                Result = "SUCCESS",
                DateFrom = now.AddDays(-1),
                DateTo = now.AddDays(1)
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BlackBoxEventListResponse>(okResult.Value);
            var item = Assert.Single(response.Items);
            Assert.Equal("SAVE", item.ActionType);
            Assert.Equal("SUCCESS", item.Result);
        }

        [Fact]
        public void Model_ContainsBlackBoxEventsTable()
        {
            using var db = CreateInMemoryDbContext();

            var entityType = db.Model.FindEntityType(typeof(BlackBoxEvent));

            Assert.NotNull(entityType);
            Assert.Equal("BLACK_BOX_EVENTS", entityType!.GetTableName());
        }

        private static SupermarketDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SupermarketDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new SupermarketDbContext(options);
        }

        private static BlackBoxController CreateController(SupermarketDbContext db, ISessionContext sessionContext)
        {
            var repository = new BlackBoxEventRepository(db);
            var service = new BlackBoxEventService(repository, new BlackBoxMetadataSanitizer(), new TestSessionContextAccessor(sessionContext));
            return new BlackBoxController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private static void AssertScreenPermission(MethodInfo? method, string expectedScreenKey)
        {
            Assert.NotNull(method);
            var attribute = method!.GetCustomAttribute<RequireScreenPermissionAttribute>();
            Assert.NotNull(attribute);

            var screenKeyField = typeof(RequireScreenPermissionAttribute)
                .GetField("_screenKey", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.Equal(expectedScreenKey, screenKeyField?.GetValue(attribute) as string);
        }

        private sealed class TestSessionContextAccessor : ISessionContextAccessor
        {
            public TestSessionContextAccessor(ISessionContext current)
            {
                Current = current;
            }

            public ISessionContext Current { get; private set; }

            public void SetContext(ISessionContext context)
            {
                Current = context;
            }
        }

        private sealed class TestSessionContext : ISessionContext
        {
            public TestSessionContext(long employeeId, long sessionId, string deviceCode, bool isAuthenticated)
            {
                EmployeeId = employeeId;
                SessionId = sessionId;
                DeviceCode = deviceCode;
                IsAuthenticated = isAuthenticated;
            }

            public long EmployeeId { get; }
            public long SessionId { get; }
            public string DeviceCode { get; }
            public bool IsAuthenticated { get; }
        }
    }
}
