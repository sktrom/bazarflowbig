using System.Threading.Tasks;

namespace Supermarket.Application.AuditLogs.Interfaces
{
    public interface IAuditLogService
    {
        Task RecordAsync(
            string action,
            string entityType,
            string? entityId = null,
            string? entityDisplayName = null,
            object? before = null,
            object? after = null,
            object? metadata = null);
    }
}
