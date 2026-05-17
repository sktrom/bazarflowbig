using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class ProductManagementRepository : IProductManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public ProductManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(long id)
        {
            return await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByBarcodeAsync(string barcode)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> CategoryExistsAsync(long categoryId)
        {
            return await _context.Categories.AnyAsync(c => c.Id == categoryId);
        }

        public async Task<bool> HasRelatedRecordsAsync(long id)
        {
            // A product is considered "used" if it is referenced in ProductBatches, InvoiceLines, or Offers
            bool hasBatches = await _context.ProductBatches.AnyAsync(pb => pb.ProductId == id);
            bool hasInvoices = await _context.Set<InvoiceLine>().AnyAsync(il => il.ProductId == id);
            bool hasOffers = await _context.Set<Offer>().AnyAsync(o => o.ProductId == id);

            return hasBatches || hasInvoices || hasOffers;
        }
    }
}
