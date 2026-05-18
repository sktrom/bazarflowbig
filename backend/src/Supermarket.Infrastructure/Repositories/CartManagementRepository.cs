using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class CartManagementRepository : ICartManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public CartManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetWorkingInvoiceByEmployeeAsync(long employeeId)
        {
            return await _context.Invoices
                .Include(i => i.OriginalEmployee)
                .Where(i => i.OriginalEmployeeId == employeeId && i.Status == InvoiceStatus.Working)
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<Invoice> CreateWorkingInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvoiceAsync(Invoice invoice)
        {
            var lines = await _context.InvoiceLines.Where(l => l.InvoiceId == invoice.Id).ToListAsync();
            var lineIds = lines.Select(l => l.Id).ToList();

            if (lineIds.Any())
            {
                var allocations = await _context.InvoiceLineBatchAllocations
                    .Where(a => lineIds.Contains(a.InvoiceLineId))
                    .ToListAsync();
                
                _context.InvoiceLineBatchAllocations.RemoveRange(allocations);
                _context.InvoiceLines.RemoveRange(lines);
            }
            
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task AddLineAsync(InvoiceLine line)
        {
            _context.InvoiceLines.Add(line);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLineAsync(InvoiceLine line)
        {
            var allocations = await _context.InvoiceLineBatchAllocations
                .Where(a => a.InvoiceLineId == line.Id)
                .ToListAsync();

            if (allocations.Any())
            {
                _context.InvoiceLineBatchAllocations.RemoveRange(allocations);
            }

            _context.InvoiceLines.Remove(line);
            await _context.SaveChangesAsync();
        }

        public async Task<System.Collections.Generic.List<InvoiceLine>> GetCartLinesAsync(long invoiceId)
        {
            return await _context.InvoiceLines
                .Include(l => l.Product)
                .Where(l => l.InvoiceId == invoiceId)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
