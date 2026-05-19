using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.AuditLogs.Interfaces
{
    public interface IAuditLogRepository
    {
        Task CreateAsync(AuditLog auditLog);
    }
}
