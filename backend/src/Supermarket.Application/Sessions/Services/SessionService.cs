using System.Threading.Tasks;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Contracts.Sessions;

namespace Supermarket.Application.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionHistoryRepository _historyRepo;
        private readonly ISessionContextAccessor _contextAccessor;

        public SessionService(ISessionHistoryRepository historyRepo, ISessionContextAccessor contextAccessor)
        {
            _historyRepo     = historyRepo;
            _contextAccessor = contextAccessor;
        }

        public async Task<SessionHistoryResponse> GetHistoryAsync(int pageIndex = 1, int pageSize = 20)
        {
            var employeeId = _contextAccessor.Current.EmployeeId;
            if (employeeId <= 0)
                throw new System.InvalidOperationException("NO_ACTIVE_SESSION");

            return await _historyRepo.GetHistoryAsync(employeeId, pageIndex, pageSize);
        }
    }
}
