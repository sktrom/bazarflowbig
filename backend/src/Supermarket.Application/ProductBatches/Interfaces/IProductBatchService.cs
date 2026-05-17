using System.Threading.Tasks;
using Supermarket.Contracts.ProductBatches;

namespace Supermarket.Application.ProductBatches.Interfaces
{
    public interface IProductBatchService
    {
        Task<BatchListResponse> GetAllByProductIdAsync(long productId);
        Task<BatchDetailResponse> CreateAsync(long productId, CreateBatchRequest request);
    }
}
