using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Application.Sessions.Services;
using Supermarket.Contracts.Sessions;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.Sessions
{
    public class SessionServiceTests
    {
        private readonly Mock<ISessionHistoryRepository> _historyRepoMock = new();
        private readonly Mock<ISessionContextAccessor> _contextAccessorMock = new();
        private readonly Mock<IAuthSessionRepository> _sessionRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogMock = new();
        private readonly SessionService _service;

        public SessionServiceTests()
        {
            _service = new SessionService(
                _historyRepoMock.Object,
                _contextAccessorMock.Object,
                _sessionRepoMock.Object,
                _auditLogMock.Object);
        }

        [Fact]
        public async Task GetActiveSessions_ReturnsMappedActiveSessions()
        {
            // Arrange
            var sessions = new List<CashSession>
            {
                new CashSession
                {
                    Id = 1,
                    EmployeeId = 10,
                    Employee = new Employee { FullName = "Emp 1", Username = "emp1" },
                    DeviceId = 20,
                    Device = new PosDevice { DeviceCode = "DEV1", DeviceName = "Device 1" },
                    StartedAt = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc),
                    LastSeenAt = new DateTime(2026, 5, 21, 11, 0, 0, DateTimeKind.Utc),
                    ExpiresAt = new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc),
                    Status = CashSessionStatus.Active
                }
            };

            _sessionRepoMock.Setup(r => r.GetActiveSessionsAsync())
                .ReturnsAsync(sessions);

            // Act
            var result = (await _service.GetActiveSessionsAsync()).ToList();

            // Assert
            Assert.Single(result);
            var activeSession = result[0];
            Assert.Equal(1, activeSession.SessionId);
            Assert.Equal(10, activeSession.EmployeeId);
            Assert.Equal("Emp 1", activeSession.EmployeeName);
            Assert.Equal("emp1", activeSession.Username);
            Assert.Equal(20, activeSession.DeviceId);
            Assert.Equal("DEV1", activeSession.DeviceCode);
            Assert.Equal("Device 1", activeSession.DeviceName);
            Assert.Equal(sessions[0].StartedAt, activeSession.StartedAt);
            Assert.Equal(sessions[0].LastSeenAt, activeSession.LastSeenAt);
            Assert.Equal(sessions[0].ExpiresAt, activeSession.ExpiresAt);
        }

        [Fact]
        public async Task ForceClose_ChangesStatusToForceClosed_AndSetsEndedAt()
        {
            // Arrange
            var session = new CashSession
            {
                Id = 1,
                Status = CashSessionStatus.Active,
                EmployeeId = 10,
                Employee = new Employee { FullName = "Emp 1", Username = "emp1" },
                Device = new PosDevice { DeviceCode = "DEV1" }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(session);

            // Act
            await _service.ForceCloseSessionAsync(1, 99, 100);

            // Assert
            _sessionRepoMock.Verify(r => r.ForceCloseAsync(1, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task ForceClose_ThrowsSessionNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((CashSession?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ForceCloseSessionAsync(999, 99, 100));

            Assert.Equal("SESSION_NOT_FOUND", ex.Message);
        }

        [Fact]
        public async Task ForceClose_ThrowsSessionNotActive_WhenSessionIsAlreadyClosed()
        {
            // Arrange
            var session = new CashSession
            {
                Id = 1,
                Status = CashSessionStatus.Closed
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(session);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ForceCloseSessionAsync(1, 99, 100));

            Assert.Equal("SESSION_NOT_ACTIVE", ex.Message);
        }

        [Fact]
        public async Task ForceClose_CallsAuditServiceWithCorrectMetadata()
        {
            // Arrange
            var session = new CashSession
            {
                Id = 1,
                Status = CashSessionStatus.Active,
                EmployeeId = 10,
                Employee = new Employee { FullName = "Emp 1", Username = "emp1" },
                Device = new PosDevice { DeviceCode = "DEV1" }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(session);

            // Act
            await _service.ForceCloseSessionAsync(1, 99, 100);

            // Assert
            _auditLogMock.Verify(a => a.RecordAsync(
                "FORCE_CLOSE_SESSION",
                "Auth",
                "1",
                "Session for Emp 1",
                null,
                null,
                It.Is<object>(meta => meta.ToString()!.Contains("closedByEmployeeId = 99") && meta.ToString()!.Contains("closedBySessionId = 100"))),
                Times.Once);
        }

        [Fact]
        public async Task ForceClose_Succeeds_EvenIfAuditServiceThrows()
        {
            // Arrange
            var session = new CashSession
            {
                Id = 1,
                Status = CashSessionStatus.Active,
                EmployeeId = 10,
                Employee = new Employee { FullName = "Emp 1", Username = "emp1" },
                Device = new PosDevice { DeviceCode = "DEV1" }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(session);

            _auditLogMock.Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Audit failed"));

            // Act
            var exception = await Record.ExceptionAsync(() => _service.ForceCloseSessionAsync(1, 99, 100));

            // Assert
            Assert.Null(exception);
            _sessionRepoMock.Verify(r => r.ForceCloseAsync(1, It.IsAny<DateTime>()), Times.Once);
        }
    }
}
