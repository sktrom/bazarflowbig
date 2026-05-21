using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.AuditLogs.Interfaces
{
    public interface IAuditLogRepository
    {
        Task CreateAsync(AuditLog auditLog);

        Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
            long? employeeId,
            string? action,
            string? entityType,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize);

        Task<AuditLog?> GetByIdAsync(long id);

        Task<(int TotalCount, DateTime? OldestCreatedAt, DateTime? NewestCreatedAt, int ApproximateLargeJsonCount)> GetStatusDetailsAsync();
    }
}

