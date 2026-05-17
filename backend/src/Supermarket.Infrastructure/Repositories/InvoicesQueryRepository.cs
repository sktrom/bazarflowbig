using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class InvoicesQueryRepository : IInvoicesQueryRepository
    {
        private readonly SupermarketDbContext _context;

        public InvoicesQueryRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<(List<(Invoice Invoice, AdjustmentRequest? LatestAdjustment)> Items, int TotalCount)> GetInvoicesPaginatedAsync(
            InvoiceStatus? status,
            long? employeeId,
            string? customerName,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? hasAdjustmentRequest,
            AdjustmentRequestStatus? adjustmentRequestStatus,
            bool? manualPriceEdited,
            string? sortBy,
            string? sortOrder,
            int page,
            int pageSize)
        {
            var invoicesQuery = _context.Invoices
                .Include(i => i.OriginalEmployee)
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.Status == status.Value);

            if (employeeId.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.OriginalEmployeeId == employeeId.Value);

            if (!string.IsNullOrWhiteSpace(customerName))
                invoicesQuery = invoicesQuery.Where(i => i.CustomerName != null && i.CustomerName.Contains(customerName));

            if (dateFrom.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.CreatedAt <= dateTo.Value);

            if (hasAdjustmentRequest.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.HasAdjustmentRequest == hasAdjustmentRequest.Value);

            if (manualPriceEdited.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.HasManualPriceEdit == manualPriceEdited.Value);

            // Left Join with Latest Adjustment Request
            var query = from inv in invoicesQuery
                        let latestAdj = _context.AdjustmentRequests
                            .Where(a => a.InvoiceId == inv.Id)
                            .OrderByDescending(a => a.CreatedAt)
                            .FirstOrDefault()
                        select new { Invoice = inv, LatestAdjustment = latestAdj };

            if (adjustmentRequestStatus.HasValue)
            {
                query = query.Where(q => q.LatestAdjustment != null && q.LatestAdjustment.Status == adjustmentRequestStatus.Value);
            }

            var totalCount = await query.CountAsync();

            // Sorting logic
            bool isDesc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                query = sortBy.ToLower() switch
                {
                    "createdat" => isDesc ? query.OrderByDescending(q => q.Invoice.CreatedAt) : query.OrderBy(q => q.Invoice.CreatedAt),
                    "totalusd" => isDesc ? query.OrderByDescending(q => q.Invoice.TotalUsd) : query.OrderBy(q => q.Invoice.TotalUsd),
                    "invoicenumber" => isDesc ? query.OrderByDescending(q => q.Invoice.InvoiceNumber) : query.OrderBy(q => q.Invoice.InvoiceNumber),
                    _ => query.OrderByDescending(q => q.Invoice.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(q => q.Invoice.CreatedAt);
            }

            var itemsRaw = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = itemsRaw.Select(q => (q.Invoice, q.LatestAdjustment)).ToList();

            return (items, totalCount);
        }

        public async Task<(Invoice? Invoice, AdjustmentRequest? LatestAdjustment)> GetInvoiceSummaryByIdAsync(long invoiceId)
        {
            var query = from inv in _context.Invoices
                            .Include(i => i.OriginalEmployee)
                            .AsNoTracking()
                        where inv.Id == invoiceId
                        let latestAdj = _context.AdjustmentRequests
                            .Where(a => a.InvoiceId == inv.Id)
                            .OrderByDescending(a => a.CreatedAt)
                            .FirstOrDefault()
                        select new { Invoice = inv, LatestAdjustment = latestAdj };

            var result = await query.FirstOrDefaultAsync();
            return result == null ? (null, null) : (result.Invoice, result.LatestAdjustment);
        }

        public async Task<(Invoice? Invoice, List<InvoiceLine> Lines, AdjustmentRequest? LatestAdjustment)> GetInvoiceDetailsByIdAsync(long invoiceId)
        {
            var summary = await GetInvoiceSummaryByIdAsync(invoiceId);
            if (summary.Invoice == null) return (null, new List<InvoiceLine>(), null);

            var lines = await _context.InvoiceLines
                .Include(l => l.Product)
                .Where(l => l.InvoiceId == invoiceId)
                .OrderBy(l => l.SortOrder)
                .AsNoTracking()
                .ToListAsync();

            return (summary.Invoice, lines, summary.LatestAdjustment);
        }
    }
}
