using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Supermarket.Api.Middleware;
using Supermarket.Application.Common.Interfaces;
using Xunit;

namespace Supermarket.IntegrationTests.ErrorHandling
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<IWebHostEnvironment> _envMock = new();
        private readonly Mock<ISessionContextAccessor> _sessionAccessorMock = new();

        public ExceptionHandlingMiddlewareTests()
        {
            _envMock.Setup(e => e.EnvironmentName).Returns("Production");
            var sessionContextMock = new Mock<ISessionContext>();
            sessionContextMock.Setup(s => s.EmployeeId).Returns(123);
            _sessionAccessorMock.Setup(s => s.Current).Returns(sessionContextMock.Object);
        }

        [Fact]
        public async Task UnexpectedException_ReturnsSafeInternalServerErrorWithTraceId()
        {
            var context = CreateContext("trace-unexpected");
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new Exception("Sensitive raw exception message"),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                _envMock.Object);

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            var json = await ReadResponseAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("INTERNAL_SERVER_ERROR", json.RootElement.GetProperty("error").GetString());
            Assert.Equal("حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.", json.RootElement.GetProperty("message").GetString());
            Assert.Equal("trace-unexpected", json.RootElement.GetProperty("traceId").GetString());
            Assert.DoesNotContain("Sensitive raw exception message", json.RootElement.GetRawText());
            Assert.DoesNotContain("StackTrace", json.RootElement.GetRawText());
        }

        [Fact]
        public async Task DbUpdateException_ReturnsSafeDatabaseErrorWithTraceId()
        {
            var context = CreateContext("trace-db");
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new DbUpdateException("SQL Server connection string or table details"),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                _envMock.Object);

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            var json = await ReadResponseAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("DATABASE_ERROR", json.RootElement.GetProperty("error").GetString());
            Assert.Equal("trace-db", json.RootElement.GetProperty("traceId").GetString());
            Assert.DoesNotContain("SQL Server connection string", json.RootElement.GetRawText());
        }

        [Fact]
        public async Task KnownInvalidOperationException_ReturnsMappedBusinessError()
        {
            var context = CreateContext("trace-known");
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("SETUP_REQUIRED"),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                _envMock.Object);

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            var json = await ReadResponseAsync(context);

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.Equal(403, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("SETUP_REQUIRED", json.RootElement.GetProperty("error").GetString());
            Assert.Equal("النظام يحتاج إلى الإعداد الأول.", json.RootElement.GetProperty("message").GetString());
            Assert.Equal("trace-known", json.RootElement.GetProperty("traceId").GetString());
        }

        [Fact]
        public async Task UnknownInvalidOperationException_DoesNotExposeRawMessage()
        {
            var context = CreateContext("trace-unknown-business");
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Raw technical domain detail"),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                _envMock.Object);

            await middleware.InvokeAsync(context, _sessionAccessorMock.Object);

            var json = await ReadResponseAsync(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("INTERNAL_SERVER_ERROR", json.RootElement.GetProperty("error").GetString());
            Assert.DoesNotContain("Raw technical domain detail", json.RootElement.GetRawText());
        }

        private static DefaultHttpContext CreateContext(string traceId)
        {
            var context = new DefaultHttpContext
            {
                TraceIdentifier = traceId
            };
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            return await JsonDocument.ParseAsync(context.Response.Body);
        }
    }
}
