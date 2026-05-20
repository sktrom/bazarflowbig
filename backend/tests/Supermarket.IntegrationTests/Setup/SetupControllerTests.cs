using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Supermarket.Api.Controllers;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.Setup;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;
using Xunit;

namespace Supermarket.IntegrationTests.Setup
{
    public class SetupControllerTests
    {
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IAuditLogService> _auditLogMock = new();

        public SetupControllerTests()
        {
            _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns((string pwd) => "hashed_" + pwd);
        }

        private SupermarketDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SupermarketDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new SupermarketDbContext(options);
        }

        [Fact]
        public async Task GetStatus_ReturnsFalse_WhenSetupCompletedSettingIsMissing()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var result = await controller.GetStatus();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = Assert.IsType<SetupStatusResponse>(okResult.Value);
            Assert.False(status.SetupCompleted);
        }

        [Fact]
        public async Task GetStatus_ReturnsFalse_WhenSetupCompletedSettingIsFalse()
        {
            var db = CreateInMemoryDbContext();
            db.AppSettings.Add(new AppSetting { SettingKey = "setup_completed", SettingValue = "false" });
            await db.SaveChangesAsync();

            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var result = await controller.GetStatus();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = Assert.IsType<SetupStatusResponse>(okResult.Value);
            Assert.False(status.SetupCompleted);
        }

        [Fact]
        public async Task GetStatus_ReturnsTrue_WhenSetupCompletedSettingIsTrue()
        {
            var db = CreateInMemoryDbContext();
            db.AppSettings.Add(new AppSetting { SettingKey = "setup_completed", SettingValue = "true" });
            await db.SaveChangesAsync();

            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var result = await controller.GetStatus();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = Assert.IsType<SetupStatusResponse>(okResult.Value);
            Assert.True(status.SetupCompleted);
        }

        [Fact]
        public async Task Complete_Rejects_Admin123Password()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Super Admin",
                AdminUsername: "admin",
                AdminPassword: "admin123",
                StoreName: "My Store",
                ExchangeRate: 15000,
                DeviceCode: "POS-1",
                DeviceName: "POS Register 1"
            );

            var result = await controller.Complete(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            dynamic errorObj = badRequest.Value!;
            Assert.Equal("INVALID_ADMIN_PASSWORD", (string)errorObj.GetType().GetProperty("error").GetValue(errorObj, null));
        }

        [Fact]
        public async Task Complete_Rejects_ExchangeRateLessThanOrEqualToZero()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Super Admin",
                AdminUsername: "admin",
                AdminPassword: "strongPassword123",
                StoreName: "My Store",
                ExchangeRate: 0,
                DeviceCode: "POS-1",
                DeviceName: "POS Register 1"
            );

            var result = await controller.Complete(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            dynamic errorObj = badRequest.Value!;
            Assert.Equal("INVALID_EXCHANGE_RATE", (string)errorObj.GetType().GetProperty("error").GetValue(errorObj, null));
        }

        [Fact]
        public async Task Complete_Rejects_EmptyRequiredFields()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "",
                AdminUsername: "admin",
                AdminPassword: "strongPassword123",
                StoreName: "My Store",
                ExchangeRate: 15000,
                DeviceCode: "POS-1",
                DeviceName: "POS Register 1"
            );

            var result = await controller.Complete(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            dynamic errorObj = badRequest.Value!;
            Assert.Equal("SETUP_VALIDATION_ERROR", (string)errorObj.GetType().GetProperty("error").GetValue(errorObj, null));
        }

        [Fact]
        public async Task Complete_CreatesFirstAdmin_IfNoEmployeesExist()
        {
            var db = CreateInMemoryDbContext();
            _passwordHasherMock.Setup(h => h.Hash("strongPassword123")).Returns("hashed_pwd");
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "New Admin",
                AdminUsername: "newadmin",
                AdminPassword: "strongPassword123",
                StoreName: "BazarFlow Store",
                ExchangeRate: 15000,
                DeviceCode: "POS-100",
                DeviceName: "Device 100"
            );

            var result = await controller.Complete(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SetupCompleteResponse>(okResult.Value);
            Assert.True(response.Success);

            var admins = await db.Employees.ToListAsync();
            Assert.Single(admins);
            Assert.Equal("New Admin", admins[0].FullName);
            Assert.Equal("newadmin", admins[0].Username);
            Assert.Equal("hashed_pwd", admins[0].PasswordHash);
        }

        [Fact]
        public async Task Complete_UpdatesSeedAdmin_WhenSafe()
        {
            var db = CreateInMemoryDbContext();
            var seedAdmin = new Employee { Id = 1, FullName = "System Seed", Username = "admin", PasswordHash = "old_hash" };
            db.Employees.Add(seedAdmin);
            await db.SaveChangesAsync();

            _passwordHasherMock.Setup(h => h.Hash("newStrongPassword")).Returns("new_hashed_pwd");
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Real Manager",
                AdminUsername: "manager",
                AdminPassword: "newStrongPassword",
                StoreName: "BazarFlow Store",
                ExchangeRate: 15000,
                DeviceCode: "POS-100",
                DeviceName: "Device 100"
            );

            var result = await controller.Complete(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SetupCompleteResponse>(okResult.Value);
            Assert.True(response.Success);

            var admins = await db.Employees.ToListAsync();
            Assert.Single(admins);
            Assert.Equal(1, admins[0].Id);
            Assert.Equal("Real Manager", admins[0].FullName);
            Assert.Equal("manager", admins[0].Username);
            Assert.Equal("new_hashed_pwd", admins[0].PasswordHash);
        }

        [Fact]
        public async Task Complete_Rejects_AmbiguousEmployeeState()
        {
            var db = CreateInMemoryDbContext();
            db.Employees.Add(new Employee { Id = 1, FullName = "Seed Admin" });
            db.Employees.Add(new Employee { Id = 2, FullName = "Other User" });
            await db.SaveChangesAsync();

            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Admin Two",
                AdminUsername: "admin2",
                AdminPassword: "strongPassword123",
                StoreName: "BazarFlow",
                ExchangeRate: 15000,
                DeviceCode: "POS-1",
                DeviceName: "Device 1"
            );

            var result = await controller.Complete(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            dynamic errorObj = badRequest.Value!;
            Assert.Equal("SETUP_STATE_AMBIGUOUS", (string)errorObj.GetType().GetProperty("error").GetValue(errorObj, null));
        }

        [Fact]
        public async Task Complete_CreatesDevice()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Super Admin",
                AdminUsername: "admin",
                AdminPassword: "strongPassword123",
                StoreName: "BazarFlow",
                ExchangeRate: 15000,
                DeviceCode: "POS-1",
                DeviceName: "Device 1"
            );

            var result = await controller.Complete(request);

            Assert.IsType<OkObjectResult>(result);
            var device = await db.PosDevices.FirstOrDefaultAsync(d => d.DeviceCode == "POS-1");
            Assert.NotNull(device);
            Assert.Equal("Device 1", device.DeviceName);
            Assert.True(device.IsActive);
        }

        [Fact]
        public async Task Complete_WritesSettingsKeys()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Super Admin",
                AdminUsername: "admin",
                AdminPassword: "strongPassword123",
                StoreName: "My Custom Store Name",
                ExchangeRate: 12500.50m,
                DeviceCode: "POS-1",
                DeviceName: "Device 1"
            );

            var result = await controller.Complete(request);

            Assert.IsType<OkObjectResult>(result);

            var storeName = await db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "store_name");
            Assert.NotNull(storeName);
            Assert.Equal("My Custom Store Name", storeName.SettingValue);

            var exchangeRate = await db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "exchange_rate_syp");
            Assert.NotNull(exchangeRate);
            Assert.Equal("12500.50", exchangeRate.SettingValue);

            var setupCompleted = await db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "setup_completed");
            Assert.NotNull(setupCompleted);
            Assert.Equal("true", setupCompleted.SettingValue);
        }

        [Fact]
        public async Task Complete_CannotRunTwice()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Super Admin",
                AdminUsername: "admin",
                AdminPassword: "strongPassword123",
                StoreName: "BazarFlow",
                ExchangeRate: 15000,
                DeviceCode: "POS-1",
                DeviceName: "Device 1"
            );

            // First run
            var result1 = await controller.Complete(request);
            Assert.IsType<OkObjectResult>(result1);

            // Second run
            var result2 = await controller.Complete(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result2);
            dynamic errorObj = badRequest.Value!;
            Assert.Equal("SETUP_ALREADY_COMPLETED", (string)errorObj.GetType().GetProperty("error").GetValue(errorObj, null));
        }

        [Fact]
        public async Task SetupAudit_DoesNotIncludePassword()
        {
            var db = CreateInMemoryDbContext();
            var controller = new SetupController(db, _passwordHasherMock.Object, _auditLogMock.Object);

            var request = new SetupCompleteRequest(
                AdminFullName: "Super Admin",
                AdminUsername: "admin",
                AdminPassword: "superSecurePassword123",
                StoreName: "BazarFlow",
                ExchangeRate: 15000,
                DeviceCode: "POS-1",
                DeviceName: "Device 1"
            );

            var result = await controller.Complete(request);
            Assert.IsType<OkObjectResult>(result);

            _auditLogMock.Verify(a => a.RecordAsync(
                "SETUP_COMPLETED",
                "System",
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                It.Is<object>(obj =>
                    !obj.ToString()!.Contains("superSecurePassword123") &&
                    !obj.ToString()!.Contains("Password") &&
                    obj.ToString()!.Contains("admin") &&
                    obj.ToString()!.Contains("POS-1")
                )
            ), Times.Once);
        }
    }
}
