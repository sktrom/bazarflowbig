using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class InventoryAllocationRepository : IInventoryAllocationRepository
    {
        private readonly SupermarketDbContext _db;

        public InventoryAllocationRepository(SupermarketDbContext db)
        {
            _db = db;
        }

        public async Task<List<InvoiceLineBatchAllocation>> GetReservedByInvoiceAsync(long invoiceId)
            => await _db.InvoiceLineBatchAllocations
                .Include(a => a.Batch)
                .Where(a => a.InvoiceLine != null
                         && a.InvoiceLine.InvoiceId == invoiceId
                         && a.AllocationStatus == AllocationStatus.Reserved)
                .ToListAsync();

        // FEFO: order by ExpiryDate ascending (nulls last), then by Id ascending
        public async Task<List<ProductBatch>> GetAvailableBatchesFEFOAsync(long productId)
            => await _db.ProductBatches
                .Where(b => b.ProductId == productId && b.QuantityAvailable > 0)
                .OrderBy(b => b.ExpiryDate == null ? DateTime.MaxValue : b.ExpiryDate)
                .ThenBy(b => b.Id)
                .ToListAsync();

        public async Task AddAllocationAsync(InvoiceLineBatchAllocation allocation)
        {
            await _db.InvoiceLineBatchAllocations.AddAsync(allocation);
        }

        public async Task UpdateAllocationAsync(InvoiceLineBatchAllocation allocation)
        {
            _db.InvoiceLineBatchAllocations.Update(allocation);
            await Task.CompletedTask;
        }

        public async Task UpdateBatchAsync(ProductBatch batch)
        {
            _db.ProductBatches.Update(batch);
            await Task.CompletedTask;
        }

        public async Task DeleteAllocationsByInvoiceAsync(long invoiceId)
        {
            var allocations = await _db.InvoiceLineBatchAllocations
                .Where(a => a.InvoiceLine != null && a.InvoiceLine.InvoiceId == invoiceId)
                .ToListAsync();
            _db.InvoiceLineBatchAllocations.RemoveRange(allocations);
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}
