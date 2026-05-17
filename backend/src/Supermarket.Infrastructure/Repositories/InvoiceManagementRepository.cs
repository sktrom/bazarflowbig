using System.Threading.Tasks;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class InvoiceManagementRepository : IInvoiceManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public InvoiceManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task CreateInvoiceLineMinimalAsync(InvoiceLine line)
        {
            _context.InvoiceLines.Add(line);
            await _context.SaveChangesAsync();
        }
    }
}
