using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Common.Services;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Application.Employees.Services;
using Supermarket.Contracts.Employees;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Employees
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeManagementRepository> _employeeRepoMock;
        private readonly Mock<IPermissionManagementRepository> _permissionRepoMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly EmployeeService _service;

        public EmployeeServiceTests()
        {
            _employeeRepoMock = new Mock<IEmployeeManagementRepository>();
            _permissionRepoMock = new Mock<IPermissionManagementRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _auditLogMock = new Mock<IAuditLogService>();
            _service = new EmployeeService(
                _employeeRepoMock.Object,
                _permissionRepoMock.Object,
                _passwordHasherMock.Object,
                _auditLogMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldHashPasswordAndSave()
        {
            // Arrange
            var request = new CreateEmployeeRequest
            {
                FullName = "Test Employee",
                Username = "testuser",
                Password = "password123"
            };
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync(request.Username)).ReturnsAsync((Employee?)null);
            _passwordHasherMock.Setup(h => h.Hash(request.Password)).Returns("hashed_password");
            _permissionRepoMock.Setup(r => r.GetFullPermissionsAsync(It.IsAny<long>())).ReturnsAsync(new List<EmployeePermissionView>());

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.Equal("Test Employee", result.FullName);
            _passwordHasherMock.Verify(h => h.Hash("password123"), Times.Once);
            _employeeRepoMock.Verify(r => r.CreateAsync(It.Is<Employee>(e => e.PasswordHash == "hashed_password")), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_ShouldUseNewHash()
        {
            var passwordHasher = new PasswordHasher();
            var service = new EmployeeService(
                _employeeRepoMock.Object,
                _permissionRepoMock.Object,
                passwordHasher,
                _auditLogMock.Object);
            Employee? createdEmployee = null;
            var request = new CreateEmployeeRequest
            {
                FullName = "Test Employee",
                Username = "testuser",
                Password = "password123"
            };
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync(request.Username)).ReturnsAsync((Employee?)null);
            _employeeRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<Employee>()))
                .Callback<Employee>(employee => createdEmployee = employee)
                .ReturnsAsync((Employee employee) => employee);
            _permissionRepoMock.Setup(r => r.GetFullPermissionsAsync(It.IsAny<long>())).ReturnsAsync(new List<EmployeePermissionView>());

            await service.CreateAsync(request);

            Assert.NotNull(createdEmployee);
            Assert.NotEqual(request.Password, createdEmployee!.PasswordHash);
            Assert.Equal(PasswordVerifyResult.Valid, passwordHasher.Verify(request.Password, createdEmployee.PasswordHash));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenUsernameExists()
        {
            // Arrange
            var request = new CreateEmployeeRequest { Username = "exists" };
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync("exists")).ReturnsAsync(new Employee());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeactivate_WhenHasRelatedRecords()
        {
            // Arrange
            long id = 1;
            long currentId = 2;
            var employee = new Employee { Id = id, IsActive = true };
            _employeeRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(employee);
            _employeeRepoMock.Setup(r => r.HasRelatedRecordsAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(id, currentId);

            // Assert
            Assert.Equal("DEACTIVATED", result.Action);
            Assert.False(employee.IsActive);
            _employeeRepoMock.Verify(r => r.UpdateAsync(employee), Times.Once);
            _employeeRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenDeletingSelf()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1, 1));
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldRecordAuditWithoutSecrets()
        {
            var employee = new Employee
            {
                Id = 7,
                FullName = "Ali",
                Username = "ali",
                PasswordHash = "old_hash"
            };
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _passwordHasherMock.Setup(h => h.Hash("new-password")).Returns("new_hash");

            await _service.ResetPasswordAsync(7, new ResetPasswordRequest { NewPassword = "new-password" });

            _employeeRepoMock.Verify(r => r.UpdateAsync(It.Is<Employee>(e => e.PasswordHash == "new_hash")), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync(
                "RESET_PASSWORD",
                "Employee",
                "7",
                "ali",
                null,
                null,
                It.Is<object>(metadata => !metadata.ToString()!.Contains("new-password", StringComparison.OrdinalIgnoreCase))),
                Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldUseNewHash()
        {
            var passwordHasher = new PasswordHasher();
            var service = new EmployeeService(
                _employeeRepoMock.Object,
                _permissionRepoMock.Object,
                passwordHasher,
                _auditLogMock.Object);
            var employee = new Employee
            {
                Id = 7,
                FullName = "Ali",
                Username = "ali",
                PasswordHash = "old_hash"
            };
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);

            await service.ResetPasswordAsync(7, new ResetPasswordRequest { NewPassword = "new-password" });

            Assert.NotEqual("new-password", employee.PasswordHash);
            Assert.Equal(PasswordVerifyResult.Valid, passwordHasher.Verify("new-password", employee.PasswordHash));
        }
    }
}
