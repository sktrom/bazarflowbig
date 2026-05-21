using System.Threading.Tasks;
using Supermarket.Contracts.Sessions;

namespace Supermarket.Application.Sessions.Interfaces
{
    public interface ISessionService
    {
        Task<SessionHistoryResponse> GetHistoryAsync(int pageIndex = 1, int pageSize = 20);
        Task<System.Collections.Generic.IEnumerable<ActiveSessionResponse>> GetActiveSessionsAsync();
        Task ForceCloseSessionAsync(long sessionId, long currentEmployeeId, long currentSessionId);
    }
}
