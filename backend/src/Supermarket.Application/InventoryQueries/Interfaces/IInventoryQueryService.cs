using System.Threading.Tasks;
using Supermarket.Contracts.InventoryQueries;

namespace Supermarket.Application.InventoryQueries.Interfaces
{
    public interface IInventoryQueryService
    {
        Task<InventoryListResponse> GetInventoryListAsync(
            string? search,
            long? categoryId,
            bool? isActive,
            bool? hasStock,
            bool? hasExpiry,
            int page,
            int pageSize);

        Task<InventoryDetailsResponse> GetInventoryDetailsAsync(long productId);
    }
}
