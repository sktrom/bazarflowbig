using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.InventoryQueries.Interfaces
{
    public interface IInventoryQueryRepository
    {
        Task<(List<(Product Product, decimal TotalQuantityAvailable, int BatchCount, DateTime? NearestExpiryDate)> Items, int TotalCount)> GetInventoryPaginatedAsync(
            string? search,
            long? categoryId,
            bool? isActive,
            bool? hasStock,
            bool? hasExpiry,
            int page,
            int pageSize);

        Task<Product?> GetProductByIdAsync(long productId);
        
        Task<List<ProductBatch>> GetProductBatchesAsync(long productId);
        
        Task<List<(Product Product, decimal TotalQuantityAvailable)>> GetProductsWithStockLevelsAsync();
        Task<List<(ProductBatch Batch, Product Product)>> GetBatchesWithExpiryAsync();
        Task<List<long>> GetProductsWithZeroSalesLast30DaysAsync();
    }
}
