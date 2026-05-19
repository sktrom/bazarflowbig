using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Suppliers.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly SupermarketDbContext _context;

        public SupplierRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(long id)
        {
            return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> ActiveNameExistsAsync(string name, long? excludeId = null)
        {
            var normalizedName = name.Trim().ToLower();
            var query = _context.Suppliers
                .Where(s => s.IsActive && s.Name.ToLower() == normalizedName);

            if (excludeId.HasValue)
                query = query.Where(s => s.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Supplier supplier)
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
        }

        public Task<bool> IsSupplierUsedAsync(long id)
        {
            return _context.PurchaseInvoices.AnyAsync(invoice => invoice.SupplierId == id);
        }
    }
}
