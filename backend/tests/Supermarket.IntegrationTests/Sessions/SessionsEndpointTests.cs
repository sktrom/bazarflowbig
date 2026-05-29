using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Supermarket.Api.Controllers;
using Supermarket.Api.Filters;
using Supermarket.Api.Middleware;
using Supermarket.Api.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Contracts.Sessions;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Application.Auth.Interfaces;
using Xunit;

namespace Supermarket.IntegrationTests.Sessions
{
    public class SessionsEndpointTests
    {
        private readonly Mock<ISessionService> _sessionServiceMock = new();
        private readonly Mock<ISessionContext> _sessionContextMock = new();

        private SessionsController CreateController()
        {
            return new SessionsController(_sessionServiceMock.Object, _sessionContextMock.Object);
        }

        [Fact]
        public async Task ActiveSessions_Endpoint_Requires_ActiveSession_And_SettingsPermission()
        {
            var controllerType = typeof(SessionsController);
            var activeSessionAttr = controllerType.GetCustomAttribute<RequireActiveSessionAttribute>();
            Assert.NotNull(activeSessionAttr);

            var method = controllerType.GetMethod(nameof(SessionsController.GetActive));
            Assert.NotNull(method);
            
            var permissionAttr = method!.GetCustomAttribute<RequireScreenPermissionAttribute>();
            Assert.NotNull(permissionAttr);

            var screenKeyField = typeof(RequireScreenPermissionAttribute).GetField("_screenKey", BindingFlags.NonPublic | BindingFlags.Instance);
            var screenKeyValue = screenKeyField?.GetValue(permissionAttr) as string;
            Assert.Equal("Devices", screenKeyValue);

            await Task.CompletedTask;
        }

        [Fact]
        public async Task ForceClose_Endpoint_Requires_ActiveSession_And_SettingsPermission()
        {
            var controllerType = typeof(SessionsController);
            var activeSessionAttr = controllerType.GetCustomAttribute<RequireActiveSessionAttribute>();
            Assert.NotNull(activeSessionAttr);

            var method = controllerType.GetMethod(nameof(SessionsController.ForceClose));
            Assert.NotNull(method);
            
            var permissionAttr = method!.GetCustomAttribute<RequireScreenPermissionAttribute>();
            Assert.NotNull(permissionAttr);

            var screenKeyField = typeof(RequireScreenPermissionAttribute).GetField("_screenKey", BindingFlags.NonPublic | BindingFlags.Instance);
            var screenKeyValue = screenKeyField?.GetValue(permissionAttr) as string;
            Assert.Equal("Devices", screenKeyValue);

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GetActive_ReturnsActiveSessions_Successfully()
        {
            // Arrange
            var expectedSessions = new List<ActiveSessionResponse>
            {
                new ActiveSessionResponse { SessionId = 1, EmployeeName = "Test Emp" }
            };
            _sessionServiceMock.Setup(s => s.GetActiveSessionsAsync())
                .ReturnsAsync(expectedSessions);

            var controller = CreateController();

            // Act
            var result = await controller.GetActive();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var sessions = Assert.IsAssignableFrom<IEnumerable<ActiveSessionResponse>>(okResult.Value);
            Assert.Single(sessions);
        }

        [Fact]
        public async Task ForceClose_ReturnsOk_OnSuccess()
        {
            // Arrange
            _sessionContextMock.Setup(c => c.EmployeeId).Returns(99);
            _sessionContextMock.Setup(c => c.SessionId).Returns(100);
            _sessionServiceMock.Setup(s => s.ForceCloseSessionAsync(1, 99, 100))
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.ForceClose(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var successProp = okResult.Value.GetType().GetProperty("success")?.GetValue(okResult.Value);
            Assert.Equal(true, successProp);
        }

        [Fact]
        public async Task ForceClose_ReturnsNotFound_IfSessionNotFound()
        {
            // Arrange
            _sessionContextMock.Setup(c => c.EmployeeId).Returns(99);
            _sessionContextMock.Setup(c => c.SessionId).Returns(100);
            _sessionServiceMock.Setup(s => s.ForceCloseSessionAsync(1, 99, 100))
                .ThrowsAsync(new KeyNotFoundException("SESSION_NOT_FOUND"));

            var controller = CreateController();

            // Act
            var result = await controller.ForceClose(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            var errorProp = notFoundResult.Value.GetType().GetProperty("error")?.GetValue(notFoundResult.Value);
            Assert.Equal("SESSION_NOT_FOUND", errorProp);
        }

        [Fact]
        public async Task ForceClose_ReturnsConflict_IfSessionNotActive()
        {
            // Arrange
            _sessionContextMock.Setup(c => c.EmployeeId).Returns(99);
            _sessionContextMock.Setup(c => c.SessionId).Returns(100);
            _sessionServiceMock.Setup(s => s.ForceCloseSessionAsync(1, 99, 100))
                .ThrowsAsync(new InvalidOperationException("SESSION_NOT_ACTIVE"));

            var controller = CreateController();

            // Act
            var result = await controller.ForceClose(1);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
            var errorProp = conflictResult.Value.GetType().GetProperty("error")?.GetValue(conflictResult.Value);
            Assert.Equal("SESSION_NOT_ACTIVE", errorProp);
        }

        [Fact]
        public async Task Middleware_ShouldRejectForceClosedSessionToken()
        {
            // Arrange
            var accessor = new SessionContextAccessor();
            var repoMock = new Mock<IAuthSessionRepository>();
            
            // GetActiveByTokenAsync will return null since the repository filters for Active sessions only
            repoMock.Setup(r => r.GetActiveByTokenAsync("force-closed-token")).ReturnsAsync((CashSession?)null);
            
            var middleware = new SessionMiddleware(_ => Task.CompletedTask);
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Session-Token"] = "force-closed-token";

            // Act
            await middleware.InvokeAsync(context, repoMock.Object, accessor);

            // Assert
            Assert.False(accessor.Current.IsAuthenticated);
        }
    }
}
