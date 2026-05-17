using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class AdjustmentRequestRepository : IAdjustmentRequestRepository
    {
        private readonly SupermarketDbContext _context;
        private IDbContextTransaction? _transaction;

        public AdjustmentRequestRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(long invoiceId)
        {
            return await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task<bool> HasPendingRequestAsync(long invoiceId)
        {
            return await _context.AdjustmentRequests
                .AnyAsync(r => r.InvoiceId == invoiceId && r.Status == AdjustmentRequestStatus.Pending);
        }

        public async Task<bool> HasRejectedRequestAsync(long invoiceId)
        {
            return await _context.AdjustmentRequests
                .AnyAsync(r => r.InvoiceId == invoiceId && r.Status == AdjustmentRequestStatus.Rejected);
        }

        public async Task<AdjustmentRequest> CreateRequestAsync(AdjustmentRequest request, List<AdjustmentRequestLine> lines)
        {
            _context.AdjustmentRequests.Add(request);
            await _context.SaveChangesAsync();

            if (lines.Any())
            {
                foreach (var line in lines)
                {
                    line.AdjustmentRequestId = request.Id;
                }
                _context.AdjustmentRequestLines.AddRange(lines);
                await _context.SaveChangesAsync();
            }

            return request;
        }

        public async Task<AdjustmentRequest?> GetRequestByIdAsync(long requestId)
        {
            return await _context.AdjustmentRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task<List<AdjustmentRequestLine>> GetRequestLinesAsync(long requestId)
        {
            return await _context.AdjustmentRequestLines
                .Where(l => l.AdjustmentRequestId == requestId)
                .ToListAsync();
        }

        public async Task<List<InvoiceLine>> GetInvoiceLinesAsync(long invoiceId)
        {
            return await _context.InvoiceLines
                .Where(l => l.InvoiceId == invoiceId)
                .ToListAsync();
        }

        public async Task UpdateRequestAsync(AdjustmentRequest request)
        {
            _context.AdjustmentRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInvoiceLineAsync(InvoiceLine invoiceLine)
        {
            _context.InvoiceLines.Update(invoiceLine);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInvoiceLinesAsync(List<InvoiceLine> invoiceLines)
        {
            _context.InvoiceLines.UpdateRange(invoiceLines);
            await _context.SaveChangesAsync();
        }

        public async Task ReleaseAllInvoiceAllocationsAsync(long invoiceId)
        {
            var allocations = await _context.InvoiceLineBatchAllocations
                .Include(a => a.Batch)
                .Include(a => a.InvoiceLine)
                .Where(a => a.InvoiceLine!.InvoiceId == invoiceId && a.AllocationStatus != AllocationStatus.Released)
                .ToListAsync();

            foreach (var alloc in allocations)
            {
                alloc.AllocationStatus = AllocationStatus.Released;
                if (alloc.Batch != null)
                {
                    alloc.Batch.QuantityAvailable += alloc.Quantity;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task ReleaseInvoiceLineAllocationsAsync(long invoiceLineId)
        {
            var allocations = await _context.InvoiceLineBatchAllocations
                .Include(a => a.Batch)
                .Where(a => a.InvoiceLineId == invoiceLineId && a.AllocationStatus != AllocationStatus.Released)
                .ToListAsync();

            foreach (var alloc in allocations)
            {
                alloc.AllocationStatus = AllocationStatus.Released;
                if (alloc.Batch != null)
                {
                    alloc.Batch.QuantityAvailable += alloc.Quantity;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task PartiallyReleaseInvoiceLineAllocationsLifoAsync(long invoiceLineId, decimal quantityToRelease)
        {
            var allocations = await _context.InvoiceLineBatchAllocations
                .Include(a => a.Batch)
                .Where(a => a.InvoiceLineId == invoiceLineId && a.AllocationStatus != AllocationStatus.Released)
                // LIFO: Release the most recently allocated first. Assuming higher Id means more recently allocated,
                // or we can sort by Id descending.
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            decimal remainingToRelease = quantityToRelease;

            foreach (var alloc in allocations)
            {
                if (remainingToRelease <= 0) break;

                if (alloc.Quantity <= remainingToRelease)
                {
                    // Full release of this allocation
                    remainingToRelease -= alloc.Quantity;
                    alloc.AllocationStatus = AllocationStatus.Released;
                    if (alloc.Batch != null)
                    {
                        alloc.Batch.QuantityAvailable += alloc.Quantity;
                    }
                }
                else
                {
                    // Partial release of this allocation
                    alloc.Quantity -= remainingToRelease;
                    if (alloc.Batch != null)
                    {
                        alloc.Batch.QuantityAvailable += remainingToRelease;
                    }
                    remainingToRelease = 0;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
