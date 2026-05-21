using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Supermarket.Api.Controllers;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.Auth;
using Xunit;

namespace Supermarket.IntegrationTests.Auth
{
    public class AuthEndpointTests
    {
        private readonly Mock<IAuthService> _authServiceMock = new();
        private readonly Mock<ISessionContext> _sessionContextMock = new();

        [Fact]
        public async Task Post_Login_InvalidLogin_ReturnsUnauthorizedLoginFailed()
        {
            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new InvalidOperationException("LOGIN_FAILED"));
            var controller = CreateController();

            var result = await controller.Login(LoginRequest());

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
            Assert.Equal("LOGIN_FAILED", GetError(unauthorized.Value!));
        }

        [Fact]
        public async Task Post_Login_RepeatedInvalidLogin_ReturnsTooManyRequestsLoginThrottled()
        {
            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new InvalidOperationException("LOGIN_THROTTLED"));
            var controller = CreateController();

            var result = await controller.Login(LoginRequest());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(429, objectResult.StatusCode);
            Assert.Equal("LOGIN_THROTTLED", GetError(objectResult.Value!));
        }

        [Fact]
        public async Task Post_Login_ActiveSession_ReturnsConflict()
        {
            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new InvalidOperationException("EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION"));
            var controller = CreateController();

            var result = await controller.Login(LoginRequest());

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION", GetError(conflict.Value!));
        }

        [Fact]
        public async Task Post_Login_SetupRequired_ReturnsForbidden()
        {
            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new InvalidOperationException("SETUP_REQUIRED"));
            var controller = CreateController();

            var result = await controller.Login(LoginRequest());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            Assert.Equal("SETUP_REQUIRED", GetError(objectResult.Value!));
        }

        [Fact]
        public async Task Post_Login_DefaultDeviceNotAllowed_ReturnsForbidden()
        {
            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new InvalidOperationException("DEFAULT_DEVICE_NOT_ALLOWED"));
            var controller = CreateController();

            var result = await controller.Login(LoginRequest());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            Assert.Equal("DEFAULT_DEVICE_NOT_ALLOWED", GetError(objectResult.Value!));
        }

        private AuthController CreateController()
        {
            return new AuthController(_authServiceMock.Object, _sessionContextMock.Object);
        }

        private static LoginRequest LoginRequest()
        {
            return new LoginRequest
            {
                Username = "cashier",
                Password = "password",
                DeviceCode = "POS-01"
            };
        }

        private static string GetError(object value)
        {
            return (string)value.GetType().GetProperty("error")!.GetValue(value)!;
        }
    }
}
