using System;
using System.Collections.Generic;
using System.Text.Json;
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
            _permissionRepoMock
                .Setup(r => r.GetFullPermissionsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<EmployeePermissionView>());

            _service = new EmployeeService(
                _employeeRepoMock.Object,
                _permissionRepoMock.Object,
                _passwordHasherMock.Object,
                _auditLogMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldHashPasswordAndSave()
        {
            var request = new CreateEmployeeRequest
            {
                FullName = "Test Employee",
                Username = "testuser",
                Password = "password123"
            };
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync(request.Username)).ReturnsAsync((Employee?)null);
            _passwordHasherMock.Setup(h => h.Hash(request.Password)).Returns("hashed_password");

            var result = await _service.CreateAsync(request);

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

            await service.CreateAsync(request);

            Assert.NotNull(createdEmployee);
            Assert.NotEqual(request.Password, createdEmployee!.PasswordHash);
            Assert.Equal(PasswordVerifyResult.Valid, passwordHasher.Verify(request.Password, createdEmployee.PasswordHash));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenUsernameExists()
        {
            var request = new CreateEmployeeRequest { Username = "exists" };
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync("exists")).ReturnsAsync(new Employee());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
            _auditLogMock.Verify(a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldRecordEmployeeCreate()
        {
            SetupCreateEmployee(7);
            _permissionRepoMock
                .Setup(r => r.GetFullPermissionsAsync(7))
                .ReturnsAsync(new List<EmployeePermissionView>
                {
                    Permission("Reports", true),
                    Permission("Sales", true),
                    Permission("Settings", false)
                });

            await _service.CreateAsync(new CreateEmployeeRequest
            {
                FullName = "Ali Ahmad",
                Username = "ali",
                Phone = "123",
                Password = "password123"
            });

            _auditLogMock.Verify(a => a.RecordAsync(
                "EMPLOYEE_CREATE",
                "Employee",
                "7",
                "ali",
                null,
                It.Is<object>(after => JsonContains(after, "\"employeeId\":7") && JsonContains(after, "\"Username\":\"ali\"")),
                It.Is<object>(metadata => JsonContains(metadata, "\"permissionCount\":3") && JsonContains(metadata, "Reports") && JsonContains(metadata, "Sales"))),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldNotAuditPasswordOrPasswordHash()
        {
            SetupCreateEmployee(7);
            object? after = null;
            object? metadata = null;
            _auditLogMock
                .Setup(a => a.RecordAsync("EMPLOYEE_CREATE", "Employee", "7", "ali", null, It.IsAny<object?>(), It.IsAny<object?>()))
                .Callback<string, string, string?, string?, object?, object?, object?>((_, _, _, _, _, capturedAfter, capturedMetadata) =>
                {
                    after = capturedAfter;
                    metadata = capturedMetadata;
                });

            await _service.CreateAsync(new CreateEmployeeRequest
            {
                FullName = "Ali Ahmad",
                Username = "ali",
                Password = "plain-secret"
            });

            var serialized = $"{JsonSerializer.Serialize(after)} {JsonSerializer.Serialize(metadata)}";
            Assert.DoesNotContain("plain-secret", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("passwordHash", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_ShouldRecordEmployeeUpdate()
        {
            var employee = ExistingEmployee();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);

            await _service.UpdateAsync(7, new UpdateEmployeeRequest
            {
                FullName = "Ali Updated",
                Phone = "999",
                IsActive = false
            });

            _auditLogMock.Verify(a => a.RecordAsync(
                "EMPLOYEE_UPDATE",
                "Employee",
                "7",
                "ali",
                It.Is<object>(before => JsonContains(before, "\"FullName\":\"Ali Ahmad\"") && JsonContains(before, "\"IsActive\":true")),
                It.Is<object>(after => JsonContains(after, "\"FullName\":\"Ali Updated\"") && JsonContains(after, "\"IsActive\":false")),
                It.Is<object>(metadata => JsonContains(metadata, "\"employeeId\":7") && JsonContains(metadata, "\"Username\":\"ali\""))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldRecordUpdatePermissions_WhenPermissionsChanged()
        {
            var employee = ExistingEmployee();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _permissionRepoMock.Setup(r => r.GetFullPermissionsAsync(7)).ReturnsAsync(new List<EmployeePermissionView>
            {
                Permission("Sales", true),
                Permission("Reports", false)
            });
            _permissionRepoMock.Setup(r => r.GetScreenIdByKeyAsync("Reports")).ReturnsAsync(1);
            _permissionRepoMock.Setup(r => r.GetScreenIdByKeyAsync("Sales")).ReturnsAsync(2);

            await _service.UpdateAsync(7, new UpdateEmployeeRequest
            {
                FullName = "Ali Ahmad",
                Phone = "123",
                IsActive = true,
                Permissions = new List<PermissionEntry>
                {
                    new() { ScreenKey = "Reports", CanAccess = true },
                    new() { ScreenKey = "Sales", CanAccess = true }
                }
            });

            _auditLogMock.Verify(a => a.RecordAsync(
                "UPDATE_PERMISSIONS",
                "Employee",
                "7",
                "ali",
                It.Is<object>(before => JsonContains(before, "Sales") && !JsonContains(before, "Reports")),
                It.Is<object>(after => JsonContains(after, "Reports") && JsonContains(after, "Sales")),
                It.Is<object>(metadata => JsonContains(metadata, "\"enabledCountBefore\":1") && JsonContains(metadata, "\"enabledCountAfter\":2"))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldNotRecordUpdatePermissions_WhenPermissionsNotChanged()
        {
            var employee = ExistingEmployee();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _permissionRepoMock.Setup(r => r.GetFullPermissionsAsync(7)).ReturnsAsync(new List<EmployeePermissionView>
            {
                Permission("Reports", true),
                Permission("Sales", true)
            });
            _permissionRepoMock.Setup(r => r.GetScreenIdByKeyAsync("Reports")).ReturnsAsync(1);
            _permissionRepoMock.Setup(r => r.GetScreenIdByKeyAsync("Sales")).ReturnsAsync(2);

            await _service.UpdateAsync(7, new UpdateEmployeeRequest
            {
                FullName = "Ali Ahmad",
                Phone = "123",
                IsActive = true,
                Permissions = new List<PermissionEntry>
                {
                    new() { ScreenKey = "Sales", CanAccess = true },
                    new() { ScreenKey = "Reports", CanAccess = true }
                }
            });

            VerifyAuditNever("UPDATE_PERMISSIONS");
        }

        [Fact]
        public async Task UpdateAsync_ShouldNotRecordUpdatePermissions_WhenPermissionsAreNull()
        {
            var employee = ExistingEmployee();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);

            await _service.UpdateAsync(7, new UpdateEmployeeRequest
            {
                FullName = "Ali Ahmad",
                Phone = "123",
                IsActive = true,
                Permissions = null
            });

            VerifyAuditNever("UPDATE_PERMISSIONS");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRecordEmployeeDeactivate_WhenHasRelatedRecords()
        {
            var employee = ExistingEmployee();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _employeeRepoMock.Setup(r => r.HasRelatedRecordsAsync(7)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(7, 99);

            Assert.Equal("DEACTIVATED", result.Action);
            _auditLogMock.Verify(a => a.RecordAsync(
                "EMPLOYEE_DEACTIVATE",
                "Employee",
                "7",
                "ali",
                It.Is<object>(before => JsonContains(before, "\"isActive\":true")),
                It.Is<object>(after => JsonContains(after, "\"isActive\":false")),
                It.Is<object>(metadata => JsonContains(metadata, "\"employeeId\":7") && JsonContains(metadata, "\"Username\":\"ali\""))),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRecordEmployeeDelete_WhenNoRelatedRecords()
        {
            var employee = ExistingEmployee();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _employeeRepoMock.Setup(r => r.HasRelatedRecordsAsync(7)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(7, 99);

            Assert.Equal("DELETED", result.Action);
            _auditLogMock.Verify(a => a.RecordAsync(
                "EMPLOYEE_DELETE",
                "Employee",
                "7",
                "ali",
                It.Is<object>(before => JsonContains(before, "\"employeeId\":7") && JsonContains(before, "\"Username\":\"ali\"")),
                null,
                null),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenDeletingSelf()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1, 1));
            VerifyAuditNever("EMPLOYEE_DELETE");
            VerifyAuditNever("EMPLOYEE_DEACTIVATE");
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldKeepExistingResetPasswordAuditWithoutPassword()
        {
            var employee = ExistingEmployee();
            employee.PasswordHash = "old_hash";
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _passwordHasherMock.Setup(h => h.Hash("new-password")).Returns("new_hash");
            object? metadata = null;
            _auditLogMock
                .Setup(a => a.RecordAsync("RESET_PASSWORD", "Employee", "7", "ali", null, null, It.IsAny<object?>()))
                .Callback<string, string, string?, string?, object?, object?, object?>((_, _, _, _, _, _, capturedMetadata) => metadata = capturedMetadata);

            await _service.ResetPasswordAsync(7, new ResetPasswordRequest { NewPassword = "new-password" });

            _employeeRepoMock.Verify(r => r.UpdateAsync(It.Is<Employee>(e => e.PasswordHash == "new_hash")), Times.Once);
            _auditLogMock.Verify(a => a.RecordAsync("RESET_PASSWORD", "Employee", "7", "ali", null, null, It.IsAny<object?>()), Times.Once);

            var serialized = JsonSerializer.Serialize(metadata);
            Assert.DoesNotContain("new-password", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("passwordHash", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EmployeeOperation_ShouldSucceed_WhenAuditThrows()
        {
            SetupCreateEmployee(7);
            _auditLogMock
                .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<object?>()))
                .ThrowsAsync(new Exception("audit failed"));

            var result = await _service.CreateAsync(new CreateEmployeeRequest
            {
                FullName = "Ali Ahmad",
                Username = "ali",
                Password = "password123"
            });

            Assert.Equal("Ali Ahmad", result.FullName);
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
            var employee = ExistingEmployee();
            employee.PasswordHash = "old_hash";
            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);

            await service.ResetPasswordAsync(7, new ResetPasswordRequest { NewPassword = "new-password" });

            Assert.NotEqual("new-password", employee.PasswordHash);
            Assert.Equal(PasswordVerifyResult.Valid, passwordHasher.Verify("new-password", employee.PasswordHash));
        }

        private void SetupCreateEmployee(long employeeId)
        {
            _employeeRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Employee?)null);
            _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");
            _employeeRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<Employee>()))
                .Callback<Employee>(employee => employee.Id = employeeId)
                .ReturnsAsync((Employee employee) => employee);
        }

        private static Employee ExistingEmployee() => new()
        {
            Id = 7,
            FullName = "Ali Ahmad",
            Username = "ali",
            Phone = "123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        private static EmployeePermissionView Permission(string screenKey, bool canAccess) => new()
        {
            ScreenId = Math.Abs(screenKey.GetHashCode()),
            ScreenKey = screenKey,
            ScreenName = screenKey,
            CanAccess = canAccess
        };

        private void VerifyAuditNever(string action)
        {
            _auditLogMock.Verify(a => a.RecordAsync(
                action,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>()), Times.Never);
        }

        private static bool JsonContains(object value, string expected)
            => JsonSerializer.Serialize(value).Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
