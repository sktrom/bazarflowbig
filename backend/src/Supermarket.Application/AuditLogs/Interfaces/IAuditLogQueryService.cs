using System;
using System.Threading.Tasks;
using Supermarket.Contracts.AuditLogs;

namespace Supermarket.Application.AuditLogs.Interfaces
{
    public interface IAuditLogQueryService
    {
        Task<AuditLogListResponse> GetPagedAsync(
            long? employeeId,
            string? action,
            string? entityType,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize);

        Task<AuditLogDetailResponse?> GetByIdAsync(long id);

        Task<AuditLogStatusResponse> GetStatusAsync();
    }
}
