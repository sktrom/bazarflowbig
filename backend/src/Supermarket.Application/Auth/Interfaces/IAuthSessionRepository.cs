using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IAuthSessionRepository
    {
        Task<CashSession?> GetActiveByEmployeeIdAsync(long employeeId);
        Task<CashSession?> GetActiveByIdAsync(long sessionId);
        Task<CashSession?> GetActiveByTokenAsync(string token);
        Task<CashSession?> GetByIdAsync(long sessionId);
        Task CreateAsync(CashSession session);
        Task TouchAsync(long sessionId, System.DateTime lastSeenAt, System.DateTime expiresAt);
        Task ExpireAsync(long sessionId, System.DateTime endedAt);
        Task CloseAsync(long sessionId, System.DateTime closedAt);
    }
}
