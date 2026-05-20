using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Supermarket.Api.Middleware;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Api.Services;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.IntegrationTests.Auth
{
    public class SessionMiddlewareTests
    {
        [Fact]
        public async Task Middleware_ShouldAcceptValidSessionToken()
        {
            var accessor = new SessionContextAccessor();
            var repoMock = new Mock<IAuthSessionRepository>();
            var session = ActiveSession(DateTime.UtcNow.AddHours(1));
            repoMock.Setup(r => r.GetActiveByTokenAsync("valid-token-abcdefghijklmnopqrstuvwxyz")).ReturnsAsync(session);
            var middleware = new SessionMiddleware(_ => Task.CompletedTask);
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Session-Token"] = "valid-token-abcdefghijklmnopqrstuvwxyz";

            await middleware.InvokeAsync(context, repoMock.Object, accessor);

            Assert.True(accessor.Current.IsAuthenticated);
            Assert.Equal(10, accessor.Current.SessionId);
            Assert.Equal(7, accessor.Current.EmployeeId);
            Assert.Equal("POS-01", accessor.Current.DeviceCode);
        }

        [Fact]
        public async Task Middleware_ShouldRejectMissingToken()
        {
            var accessor = new SessionContextAccessor();
            var repoMock = new Mock<IAuthSessionRepository>();
            var middleware = new SessionMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(new DefaultHttpContext(), repoMock.Object, accessor);

            Assert.False(accessor.Current.IsAuthenticated);
            repoMock.Verify(r => r.GetActiveByTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Middleware_ShouldRejectInvalidToken()
        {
            var accessor = new SessionContextAccessor();
            var repoMock = new Mock<IAuthSessionRepository>();
            repoMock.Setup(r => r.GetActiveByTokenAsync("invalid-token-abcdefghijklmnopqrstuvwxyz")).ReturnsAsync((CashSession?)null);
            var middleware = new SessionMiddleware(_ => Task.CompletedTask);
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Session-Token"] = "invalid-token-abcdefghijklmnopqrstuvwxyz";

            await middleware.InvokeAsync(context, repoMock.Object, accessor);

            Assert.False(accessor.Current.IsAuthenticated);
        }

        [Fact]
        public async Task Middleware_ShouldRejectExpiredTokenAndForceCloseSession()
        {
            var accessor = new SessionContextAccessor();
            var repoMock = new Mock<IAuthSessionRepository>();
            var session = ActiveSession(DateTime.UtcNow.AddMinutes(-1));
            repoMock.Setup(r => r.GetActiveByTokenAsync("expired-token-abcdefghijklmnopqrstuvwxyz")).ReturnsAsync(session);
            var middleware = new SessionMiddleware(_ => Task.CompletedTask);
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Session-Token"] = "expired-token-abcdefghijklmnopqrstuvwxyz";

            await middleware.InvokeAsync(context, repoMock.Object, accessor);

            Assert.False(accessor.Current.IsAuthenticated);
            repoMock.Verify(r => r.ExpireAsync(session.Id, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task Middleware_ShouldTouchValidSessionWhenLastSeenIsOld()
        {
            var accessor = new SessionContextAccessor();
            var repoMock = new Mock<IAuthSessionRepository>();
            var session = ActiveSession(DateTime.UtcNow.AddHours(1));
            session.LastSeenAt = DateTime.UtcNow.AddMinutes(-2);
            repoMock.Setup(r => r.GetActiveByTokenAsync("valid-token-abcdefghijklmnopqrstuvwxyz")).ReturnsAsync(session);
            var middleware = new SessionMiddleware(_ => Task.CompletedTask);
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Session-Token"] = "valid-token-abcdefghijklmnopqrstuvwxyz";

            await middleware.InvokeAsync(context, repoMock.Object, accessor);

            repoMock.Verify(r => r.TouchAsync(session.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        private static CashSession ActiveSession(DateTime expiresAt)
        {
            return new CashSession
            {
                Id = 10,
                EmployeeId = 7,
                DeviceId = 3,
                Device = new PosDevice { Id = 3, DeviceCode = "POS-01" },
                StartedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = expiresAt,
                LastSeenAt = DateTime.UtcNow,
                Status = CashSessionStatus.Active
            };
        }
    }
}
