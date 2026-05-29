using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Supermarket.Api.Middleware;
using Supermarket.Api.Models;
using Supermarket.Application.Common.Interfaces;
using Xunit;

namespace Supermarket.ApiTests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();
        private readonly Mock<IWebHostEnvironment> _envMock = new();
        private readonly Mock<ISessionContextAccessor> _sessionAccessorMock = new();

        public ExceptionHandlingMiddlewareTests()
        {
            _envMock.Setup(e => e.EnvironmentName).Returns("Production");
            var sessionContextMock = new Mock<ISessionContext>();
            sessionContextMock.Setup(s => s.EmployeeId).Returns(123);
            _sessionAccessorMock.Setup(s => s.Current).Returns(sessionContextMock.Object);
        }

        private ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
        {
            return new ExceptionHandlingMiddleware(next, _loggerMock.Object, _envMock.Object);
        }

        private HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "test-trace-id";
            context.Request.Path = "/api/test";
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();
            return context;
        }

        private async Task<ApiErrorResponse?> GetResponseAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ApiErrorResponse>(json);
        }

        [Fact]
        public async Task InvokeAsync_UnhandledException_Returns500AndHidesDetailsInProduction()
        {
            var middleware = CreateMiddleware(ctx => throw new Exception("Secret exception details"));
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            var response = await GetResponseAsync(context);
            
            Assert.NotNull(response);
            Assert.Equal(500, response.Status);
            Assert.Equal("INTERNAL_SERVER_ERROR", response.Error);
            Assert.Equal("test-trace-id", response.TraceId);
            Assert.Null(response.Details);
            Assert.DoesNotContain("Secret exception details", response.Message);
        }

        [Fact]
        public async Task InvokeAsync_UnhandledException_ReturnsDetailsInDevelopment()
        {
            _envMock.Setup(e => e.EnvironmentName).Returns("Development");
            var middleware = CreateMiddleware(ctx => throw new Exception("Secret exception details"));
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            var response = await GetResponseAsync(context);
            
            Assert.NotNull(response);
            Assert.Contains("Secret exception details", response.Details);
        }

        [Fact]
        public async Task InvokeAsync_KeyNotFoundException_Returns404()
        {
            var middleware = CreateMiddleware(ctx => throw new KeyNotFoundException());
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            var response = await GetResponseAsync(context);
            
            Assert.NotNull(response);
            Assert.Equal(404, response.Status);
            Assert.Equal("NOT_FOUND", response.Error);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Returns400()
        {
            var middleware = CreateMiddleware(ctx => throw new ArgumentException());
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            var response = await GetResponseAsync(context);
            
            Assert.NotNull(response);
            Assert.Equal(400, response.Status);
            Assert.Equal("BAD_REQUEST", response.Error);
        }
        
        [Fact]
        public async Task InvokeAsync_DbUpdateException_Returns500AndSafeMessage()
        {
            var middleware = CreateMiddleware(ctx => throw new DbUpdateException("Internal DB Error"));
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            var response = await GetResponseAsync(context);
            
            Assert.NotNull(response);
            Assert.Equal("DATABASE_ERROR", response.Error);
            Assert.DoesNotContain("Internal DB Error", response.Message);
        }

        [Fact]
        public async Task InvokeAsync_KnownBusinessError_ReturnsMappedResponse()
        {
            var middleware = CreateMiddleware(ctx => throw new InvalidOperationException("LOGIN_THROTTLED"));
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            var response = await GetResponseAsync(context);
            
            Assert.NotNull(response);
            Assert.Equal(429, response.Status);
            Assert.Equal("LOGIN_THROTTLED", response.Error);
            Assert.Equal("تم تجاوز عدد المحاولات المسموح. حاول بعد عدة دقائق.", response.Message);
        }
    }
}
