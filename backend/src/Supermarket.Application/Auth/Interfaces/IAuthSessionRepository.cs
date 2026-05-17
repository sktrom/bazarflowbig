using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IAuthSessionRepository
    {
        Task<CashSession?> GetActiveByEmployeeIdAsync(long employeeId);
        Task<CashSession?> GetActiveByIdAsync(long sessionId);
        Task<CashSession?> GetByIdAsync(long sessionId);
        Task CreateAsync(CashSession session);
        Task CloseAsync(long sessionId, System.DateTime closedAt);
    }
}
