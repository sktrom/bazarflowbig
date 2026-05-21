using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Contracts.Sessions;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionHistoryRepository _historyRepo;
        private readonly ISessionContextAccessor _contextAccessor;
        private readonly IAuthSessionRepository _sessionRepo;
        private readonly IAuditLogService _auditLogService;

        public SessionService(
            ISessionHistoryRepository historyRepo,
            ISessionContextAccessor contextAccessor,
            IAuthSessionRepository sessionRepo,
            IAuditLogService auditLogService)
        {
            _historyRepo     = historyRepo;
            _contextAccessor = contextAccessor;
            _sessionRepo     = sessionRepo;
            _auditLogService = auditLogService;
        }

        public async Task<SessionHistoryResponse> GetHistoryAsync(int pageIndex = 1, int pageSize = 20)
        {
            var employeeId = _contextAccessor.Current.EmployeeId;
            if (employeeId <= 0)
                throw new System.InvalidOperationException("NO_ACTIVE_SESSION");

            return await _historyRepo.GetHistoryAsync(employeeId, pageIndex, pageSize);
        }

        public async Task<IEnumerable<ActiveSessionResponse>> GetActiveSessionsAsync()
        {
            var sessions = await _sessionRepo.GetActiveSessionsAsync();
            return sessions.Select(s => new ActiveSessionResponse
            {
                SessionId = s.Id,
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee?.FullName ?? string.Empty,
                Username = s.Employee?.Username ?? string.Empty,
                DeviceId = s.DeviceId,
                DeviceCode = s.Device?.DeviceCode ?? string.Empty,
                DeviceName = s.Device?.DeviceName ?? string.Empty,
                StartedAt = s.StartedAt,
                LastSeenAt = s.LastSeenAt,
                ExpiresAt = s.ExpiresAt
            });
        }

        public async Task ForceCloseSessionAsync(long sessionId, long currentEmployeeId, long currentSessionId)
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session == null)
            {
                throw new KeyNotFoundException("SESSION_NOT_FOUND");
            }

            if (session.Status != CashSessionStatus.Active)
            {
                throw new InvalidOperationException("SESSION_NOT_ACTIVE");
            }

            await _sessionRepo.ForceCloseAsync(sessionId, DateTime.UtcNow);

            try
            {
                var metadata = new
                {
                    sessionId = session.Id,
                    targetEmployeeId = session.EmployeeId,
                    targetEmployeeName = session.Employee?.FullName ?? string.Empty,
                    deviceCode = session.Device?.DeviceCode ?? string.Empty,
                    closedByEmployeeId = currentEmployeeId,
                    closedBySessionId = currentSessionId
                };

                await _auditLogService.RecordAsync(
                    "FORCE_CLOSE_SESSION",
                    "Auth",
                    session.Id.ToString(),
                    entityDisplayName: $"Session for {session.Employee?.FullName ?? string.Empty}",
                    metadata: metadata
                );
            }
            catch
            {
                // audit failure does not fail force close
            }
        }
    }
}
