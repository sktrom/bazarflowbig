using System.Threading.Tasks;
using Supermarket.Contracts.Sessions;

namespace Supermarket.Application.Sessions.Interfaces
{
    public interface ISessionService
    {
        // employeeId is taken from ISessionContext inside the service; no external parameter
        Task<SessionHistoryResponse> GetHistoryAsync(int pageIndex = 1, int pageSize = 20);
    }
}
