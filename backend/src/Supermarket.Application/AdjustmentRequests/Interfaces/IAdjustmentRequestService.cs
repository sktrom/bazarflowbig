using System.Threading.Tasks;
using Supermarket.Contracts.AdjustmentRequests;

namespace Supermarket.Application.AdjustmentRequests.Interfaces
{
    public interface IAdjustmentRequestService
    {
        Task<AdjustmentRequestResponseDto> CreateAdjustmentRequestAsync(long invoiceId, CreateAdjustmentRequestDto requestDto);
        Task<AdjustmentRequestResponseDto> ApproveAdjustmentRequestAsync(long invoiceId, long requestId);
        Task<AdjustmentRequestResponseDto> RejectAdjustmentRequestAsync(long invoiceId, long requestId);
        Task<AdjustmentRequestResponseDto?> GetAdjustmentRequestAsync(long invoiceId, long requestId);
    }
}
