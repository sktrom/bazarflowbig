using System.Threading.Tasks;
using Supermarket.Contracts.BlackBox;

namespace Supermarket.Application.BlackBox.Interfaces
{
    public interface IBlackBoxEventService
    {
        Task<CreateBlackBoxEventResponse> CreateAsync(
            CreateBlackBoxEventRequest request,
            string? ipAddress,
            string? userAgent);

        Task<BlackBoxEventListResponse> GetPagedAsync(BlackBoxEventQuery query);
        Task<BlackBoxEventDetailResponse?> GetByIdAsync(long id);
    }
}
