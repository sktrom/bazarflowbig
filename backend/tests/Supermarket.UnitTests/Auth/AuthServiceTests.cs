using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Auth.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.Auth;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Auth
{
    public class AuthServiceTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepoMock = new();
        private readonly Mock<IDeviceRepository> _deviceRepoMock = new();
        private readonly Mock<IAuthSessionRepository> _sessionRepoMock = new();
        private readonly Mock<IEmployeeScreenPermissionRepository> _permissionRepoMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<ISessionContextAccessor> _contextAccessorMock = new();
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _service = new AuthService(
                _employeeRepoMock.Object,
                _deviceRepoMock.Object,
                _sessionRepoMock.Object,
                _permissionRepoMock.Object,
                _passwordHasherMock.Object,
                _contextAccessorMock.Object);
        }

        [Fact]
        public async Task Login_WithValidNewHash_ShouldSucceed()
        {
            var employee = ActiveEmployee();
            SetupSuccessfulLoginDependencies(employee);
            _passwordHasherMock
                .Setup(h => h.Verify("password", employee.PasswordHash))
                .Returns(PasswordVerifyResult.Valid);

            var result = await _service.LoginAsync(LoginRequest());

            Assert.Equal(employee.Id, result.EmployeeId);
            _employeeRepoMock.Verify(
                r => r.UpdatePasswordHashAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_WithLegacySha256_ShouldRehashPassword()
        {
            var employee = ActiveEmployee();
            SetupSuccessfulLoginDependencies(employee);
            _passwordHasherMock
                .Setup(h => h.Verify("password", employee.PasswordHash))
                .Returns(PasswordVerifyResult.ValidNeedsRehash);
            _passwordHasherMock
                .Setup(h => h.Hash("password"))
                .Returns("identity_hash");

            var result = await _service.LoginAsync(LoginRequest());

            Assert.Equal(employee.Id, result.EmployeeId);
            Assert.Equal("identity_hash", employee.PasswordHash);
            _employeeRepoMock.Verify(
                r => r.UpdatePasswordHashAsync(employee.Id, "identity_hash", It.IsAny<DateTime>()),
                Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ShouldNotRehashAndShouldFail()
        {
            var employee = ActiveEmployee();
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync("cashier")).ReturnsAsync(employee);
            _passwordHasherMock
                .Setup(h => h.Verify("wrong", employee.PasswordHash))
                .Returns(PasswordVerifyResult.Invalid);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.LoginAsync(LoginRequest(password: "wrong")));

            Assert.Equal("INVALID_CREDENTIALS", ex.Message);
            _employeeRepoMock.Verify(
                r => r.UpdatePasswordHashAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        private void SetupSuccessfulLoginDependencies(Employee employee)
        {
            var device = new PosDevice
            {
                Id = 4,
                DeviceCode = "POS-01",
                IsActive = true
            };

            _employeeRepoMock.Setup(r => r.GetByUsernameAsync("cashier")).ReturnsAsync(employee);
            _deviceRepoMock.Setup(r => r.GetByCodeAsync("POS-01")).ReturnsAsync(device);
            _sessionRepoMock.Setup(r => r.GetActiveByEmployeeIdAsync(employee.Id)).ReturnsAsync((CashSession?)null);
            _sessionRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<CashSession>()))
                .Callback<CashSession>(session => session.Id = 99)
                .Returns(Task.CompletedTask);
            _permissionRepoMock
                .Setup(r => r.GetAllowedScreenKeysAsync(employee.Id))
                .ReturnsAsync(new List<string> { "Sales" });
        }

        private static Employee ActiveEmployee()
        {
            return new Employee
            {
                Id = 7,
                Username = "cashier",
                FullName = "Cashier User",
                PasswordHash = "stored_hash",
                IsActive = true
            };
        }

        private static LoginRequest LoginRequest(string password = "password")
        {
            return new LoginRequest
            {
                Username = "cashier",
                Password = password,
                DeviceCode = "POS-01"
            };
        }
    }
}
