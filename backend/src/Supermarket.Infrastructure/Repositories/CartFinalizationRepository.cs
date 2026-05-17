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
    public class CartFinalizationRepository : ICartFinalizationRepository
    {
        private readonly SupermarketDbContext _db;

        public CartFinalizationRepository(SupermarketDbContext db)
        {
            _db = db;
        }

        public async Task<Invoice?> GetWorkingInvoiceByEmployeeAsync(long employeeId)
            => await _db.Invoices
                .FirstOrDefaultAsync(i => i.OriginalEmployeeId == employeeId
                                       && i.Status == InvoiceStatus.Working);

        public async Task<Invoice?> GetSuspendedInvoiceByIdAsync(long invoiceId)
            => await _db.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId
                                       && i.Status == InvoiceStatus.Suspended);

        public async Task<bool> EmployeeHasNonEmptyWorkingCartAsync(long employeeId)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.OriginalEmployeeId == employeeId
                                       && i.Status == InvoiceStatus.Working);
            if (invoice == null) return false;

            return await _db.InvoiceLines.AnyAsync(l => l.InvoiceId == invoice.Id);
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _db.Invoices.Update(invoice);
            await Task.CompletedTask;
        }

        public async Task DeleteInvoiceWithLinesAsync(long invoiceId)
        {
            // Delete lines first (FK constraint), then invoice
            var lines = await _db.InvoiceLines.Where(l => l.InvoiceId == invoiceId).ToListAsync();
            _db.InvoiceLines.RemoveRange(lines);

            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice != null) _db.Invoices.Remove(invoice);
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}
