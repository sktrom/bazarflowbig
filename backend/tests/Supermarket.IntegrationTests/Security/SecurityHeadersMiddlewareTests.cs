using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Supermarket.Api.Middleware;
using Xunit;

namespace Supermarket.IntegrationTests.Security
{
    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsBasicSecurityHeaders()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.EnvironmentName).Returns("Development");

            var middleware = new SecurityHeadersMiddleware(next, mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
            Assert.Equal("camera=(), microphone=(), geolocation=(), payment=()", context.Response.Headers["Permissions-Policy"]);
        }

        [Fact]
        public async Task InvokeAsync_InDevelopment_DoesNotAddCSPReportOnly()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/any-endpoint";
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.EnvironmentName).Returns("Development");

            var middleware = new SecurityHeadersMiddleware((ctx) => Task.CompletedTask, mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"));
        }

        [Fact]
        public async Task InvokeAsync_InProduction_AddsCSPReportOnly()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/any-endpoint";
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.EnvironmentName).Returns("Production");

            var middleware = new SecurityHeadersMiddleware((ctx) => Task.CompletedTask, mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"));
            var csp = context.Response.Headers["Content-Security-Policy-Report-Only"].ToString();
            Assert.Contains("default-src 'self'", csp);
            Assert.Contains("style-src 'self' 'unsafe-inline'", csp);
            Assert.Contains("connect-src 'self'", csp);
            Assert.Contains("frame-ancestors 'none'", csp);
        }

        [Fact]
        public async Task InvokeAsync_InProduction_SwaggerPath_DoesNotAddCSPReportOnly()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/swagger/index.html";
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.EnvironmentName).Returns("Production");

            var middleware = new SecurityHeadersMiddleware((ctx) => Task.CompletedTask, mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"));
        }
    }
}
