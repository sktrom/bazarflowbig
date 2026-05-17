using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.CartFinalization.Interfaces
{
    public interface IInventoryAllocationRepository
    {
        // Returns allocations grouped by invoice line (with Batch navigation loaded)
        Task<List<InvoiceLineBatchAllocation>> GetReservedByInvoiceAsync(long invoiceId);

        // FEFO-ordered available batches for a product
        Task<List<ProductBatch>> GetAvailableBatchesFEFOAsync(long productId);

        Task AddAllocationAsync(InvoiceLineBatchAllocation allocation);
        Task UpdateAllocationAsync(InvoiceLineBatchAllocation allocation);
        Task UpdateBatchAsync(ProductBatch batch);
        Task DeleteAllocationsByInvoiceAsync(long invoiceId);
        Task SaveChangesAsync();
    }
}
