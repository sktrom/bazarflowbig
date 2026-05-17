using System.Threading.Tasks;
using Supermarket.Contracts.Sessions;

namespace Supermarket.Application.Sessions.Interfaces
{
    public interface ISessionHistoryRepository
    {
        Task<SessionHistoryResponse> GetHistoryAsync(long employeeId, int pageIndex, int pageSize);
    }
}
