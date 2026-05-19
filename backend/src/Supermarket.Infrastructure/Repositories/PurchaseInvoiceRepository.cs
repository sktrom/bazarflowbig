using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.PurchaseInvoices.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class PurchaseInvoiceRepository : IPurchaseInvoiceRepository
    {
        private readonly SupermarketDbContext _context;

        public PurchaseInvoiceRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PurchaseInvoice>> GetAllAsync()
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Supplier)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<PurchaseInvoice?> GetByIdWithDetailsAsync(long id)
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Supplier)
                .Include(i => i.CreatedByEmployee)
                .Include(i => i.Lines)
                    .ThenInclude(line => line.Product)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<PurchaseInvoice?> GetByIdForUpdateAsync(long id)
        {
            return await _context.PurchaseInvoices.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<PurchaseInvoice> CreateAsync(PurchaseInvoice invoice)
        {
            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateAsync(PurchaseInvoice invoice)
        {
            _context.PurchaseInvoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PurchaseInvoice invoice)
        {
            _context.PurchaseInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteLinesAsync(long purchaseInvoiceId)
        {
            var lines = await _context.PurchaseInvoiceLines
                .Where(line => line.PurchaseInvoiceId == purchaseInvoiceId)
                .ToListAsync();
            _context.PurchaseInvoiceLines.RemoveRange(lines);
            await _context.SaveChangesAsync();
        }

        public async Task<PurchaseInvoiceLine> AddLineAsync(PurchaseInvoiceLine line)
        {
            _context.PurchaseInvoiceLines.Add(line);
            await _context.SaveChangesAsync();
            return line;
        }

        public async Task<PurchaseInvoiceLine?> GetLineAsync(long purchaseInvoiceId, long lineId)
        {
            return await _context.PurchaseInvoiceLines
                .Include(line => line.Product)
                .FirstOrDefaultAsync(line => line.PurchaseInvoiceId == purchaseInvoiceId && line.Id == lineId);
        }

        public async Task DeleteLineAsync(PurchaseInvoiceLine line)
        {
            _context.PurchaseInvoiceLines.Remove(line);
            await _context.SaveChangesAsync();
        }

        public async Task<Supplier?> GetSupplierAsync(long supplierId)
        {
            return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        }

        public async Task<Product?> GetProductAsync(long productId)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<IReadOnlyList<Product>> LookupProductsAsync(string? search, int limit)
        {
            var query = _context.Products.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.Barcode.Contains(term));
            }

            return await query
                .OrderBy(p => p.Name)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetInvoiceCountForDateAsync(DateTime dateUtc)
        {
            var nextDate = dateUtc.Date.AddDays(1);
            return await _context.PurchaseInvoices
                .CountAsync(i => i.CreatedAt >= dateUtc.Date && i.CreatedAt < nextDate);
        }

        public async Task<int> GetNextLineSortOrderAsync(long purchaseInvoiceId)
        {
            var currentMax = await _context.PurchaseInvoiceLines
                .Where(line => line.PurchaseInvoiceId == purchaseInvoiceId)
                .Select(line => (int?)line.SortOrder)
                .MaxAsync();

            return (currentMax ?? 0) + 1;
        }

        public async Task RecalculateTotalsAsync(long purchaseInvoiceId)
        {
            var total = await _context.PurchaseInvoiceLines
                .Where(line => line.PurchaseInvoiceId == purchaseInvoiceId)
                .SumAsync(line => (decimal?)line.LineTotalUsd) ?? 0m;

            var invoice = await _context.PurchaseInvoices.FirstAsync(i => i.Id == purchaseInvoiceId);
            invoice.SubtotalUsd = total;
            invoice.TotalUsd = total;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await operation();
            await transaction.CommitAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
