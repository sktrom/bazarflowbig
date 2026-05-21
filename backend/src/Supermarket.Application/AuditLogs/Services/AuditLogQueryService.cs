using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Contracts.AuditLogs;

namespace Supermarket.Application.AuditLogs.Services
{
    public class AuditLogQueryService : IAuditLogQueryService
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogQueryService(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<AuditLogListResponse> GetPagedAsync(
            long? employeeId,
            string? action,
            string? entityType,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            var (items, totalCount) = await _repository.GetPagedAsync(
                employeeId,
                action,
                entityType,
                dateFrom,
                dateTo,
                page,
                pageSize);

            return new AuditLogListResponse
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items.Select(x => new AuditLogListItem
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee?.FullName,
                    SessionId = x.SessionId,
                    Action = x.Action,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    EntityDisplayName = x.EntityDisplayName,
                    CreatedAt = x.CreatedAt,
                    HasBefore = !string.IsNullOrEmpty(x.BeforeJson),
                    HasAfter = !string.IsNullOrEmpty(x.AfterJson),
                    HasMetadata = !string.IsNullOrEmpty(x.MetadataJson)
                }).ToList()
            };
        }

        public async Task<AuditLogDetailResponse?> GetByIdAsync(long id)
        {
            var log = await _repository.GetByIdAsync(id);
            if (log == null) return null;

            return new AuditLogDetailResponse
            {
                Id = log.Id,
                EmployeeId = log.EmployeeId,
                EmployeeName = log.Employee?.FullName,
                SessionId = log.SessionId,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                EntityDisplayName = log.EntityDisplayName,
                CreatedAt = log.CreatedAt,
                HasBefore = !string.IsNullOrEmpty(log.BeforeJson),
                HasAfter = !string.IsNullOrEmpty(log.AfterJson),
                HasMetadata = !string.IsNullOrEmpty(log.MetadataJson),
                BeforeJson = log.BeforeJson,
                AfterJson = log.AfterJson,
                MetadataJson = log.MetadataJson,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent
            };
        }

        public async Task<AuditLogStatusResponse> GetStatusAsync()
        {
            var (totalCount, oldestCreatedAt, newestCreatedAt, approximateLargeJsonCount) = 
                await _repository.GetStatusDetailsAsync();

            return new AuditLogStatusResponse
            {
                TotalCount = totalCount,
                OldestCreatedAt = oldestCreatedAt,
                NewestCreatedAt = newestCreatedAt,
                ApproximateLargeJsonCount = approximateLargeJsonCount,
                RecommendedRetentionDays = 180,
                CleanupEnabled = false
            };
        }
    }
}
