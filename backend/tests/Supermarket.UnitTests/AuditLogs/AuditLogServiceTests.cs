using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.AuditLogs.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.AuditLogs
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IAuditLogRepository> _repoMock = new();
        private readonly Mock<ISessionContext> _sessionMock = new();

        [Fact]
        public async Task RecordAsync_ShouldCreateAuditLog_WithEmployeeAndSession()
        {
            _sessionMock.Setup(s => s.IsAuthenticated).Returns(true);
            _sessionMock.Setup(s => s.EmployeeId).Returns(5);
            _sessionMock.Setup(s => s.SessionId).Returns(9);
            AuditLog? captured = null;
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .Returns(Task.CompletedTask);

            var service = new AuditLogService(_repoMock.Object, _sessionMock.Object);
            await service.RecordAsync("CREATE_BACKUP", "SystemBackup", metadata: new { fileName = "backup.bak" });

            Assert.NotNull(captured);
            Assert.Equal(5, captured!.EmployeeId);
            Assert.Equal(9, captured.SessionId);
            Assert.Equal("CREATE_BACKUP", captured.Action);
            Assert.Equal("SystemBackup", captured.EntityType);
            Assert.Contains("backup.bak", captured.MetadataJson);
        }

        [Fact]
        public async Task RecordAsync_ShouldHandleUnauthenticatedSession()
        {
            _sessionMock.Setup(s => s.IsAuthenticated).Returns(false);
            AuditLog? captured = null;
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .Returns(Task.CompletedTask);

            var service = new AuditLogService(_repoMock.Object, _sessionMock.Object);
            await service.RecordAsync("RESET_PASSWORD", "Employee");

            Assert.NotNull(captured);
            Assert.Null(captured!.EmployeeId);
            Assert.Null(captured.SessionId);
        }

        [Fact]
        public void SerializeSanitized_ShouldRemoveSensitiveFields()
        {
            var json = AuditLogService.SerializeSanitized(new Dictionary<string, object?>
            {
                ["userName"] = "ali",
                ["password"] = "secret",
                ["newPassword"] = "secret2",
                ["passwordHash"] = "hash",
                ["token"] = "token",
                ["connectionString"] = "Server=.;Password=123",
                ["nested"] = new Dictionary<string, object?> { ["apiSecret"] = "hidden", ["safe"] = "shown" }
            });

            Assert.NotNull(json);
            Assert.Contains("ali", json);
            Assert.Contains("shown", json);
            Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("connectionString", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RecordAsync_ShouldNotThrow_WhenRepositoryFails()
        {
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
                .ThrowsAsync(new Exception("db failed"));

            var service = new AuditLogService(_repoMock.Object, _sessionMock.Object);

            await service.RecordAsync("CREATE_BACKUP", "SystemBackup");
        }
    }
}
