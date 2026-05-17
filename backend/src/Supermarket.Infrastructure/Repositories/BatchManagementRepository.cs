using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.ProductBatches.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class BatchManagementRepository : IBatchManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public BatchManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProductBatch>> GetAllByProductIdAsync(long productId)
        {
            return await _context.ProductBatches
                .Where(b => b.ProductId == productId)
                .ToListAsync();
        }

        public async Task<ProductBatch> CreateAsync(ProductBatch batch)
        {
            _context.ProductBatches.Add(batch);
            await _context.SaveChangesAsync();
            return batch;
        }

        public async Task<bool> ProductExistsAsync(long productId)
        {
            return await _context.Products.AnyAsync(p => p.Id == productId);
        }

        public async Task<IReadOnlyList<ProductBatch>> GetBatchesForFeFoRoutingAsync(long productId)
        {
            // FEFO helper: Orders batches by ExpiryDate ascending (closest to expire first), 
            // putting nulls (no expiry) at the very end.
            return await _context.ProductBatches
                .Where(b => b.ProductId == productId && b.QuantityAvailable > 0)
                .OrderByDescending(b => b.ExpiryDate.HasValue) // false (null) comes after true
                .ThenBy(b => b.ExpiryDate)
                .ThenBy(b => b.EntryDate) // Fallback sorting
                .ToListAsync();
        }
    }
}
