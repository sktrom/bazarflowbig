using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.ProductBatches.Interfaces
{
    public interface IBatchManagementRepository
    {
        Task<IReadOnlyList<ProductBatch>> GetAllByProductIdAsync(long productId);
        Task<ProductBatch> CreateAsync(ProductBatch batch);
        Task<bool> ProductExistsAsync(long productId);
        Task<IReadOnlyList<ProductBatch>> GetBatchesForFeFoRoutingAsync(long productId);
    }
}
