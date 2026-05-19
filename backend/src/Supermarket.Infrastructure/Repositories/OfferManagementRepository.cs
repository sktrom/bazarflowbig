using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class OfferManagementRepository : IOfferManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public OfferManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Offer>> GetAllAsync()
        {
            return await _context.Offers
                .Include(o => o.Product)
                .ToListAsync();
        }

        public async Task<Offer?> GetByIdAsync(long id)
        {
            return await _context.Offers
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Offer> CreateAsync(Offer offer)
        {
            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task UpdateAsync(Offer offer)
        {
            _context.Offers.Update(offer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer != null)
            {
                _context.Offers.Remove(offer);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ProductExistsAsync(long productId)
        {
            return await _context.Products.AnyAsync(p => p.Id == productId);
        }

        public async Task<bool> HasInvoiceLinesAsync(long offerId)
        {
            return await _context.InvoiceLines.AnyAsync(il => il.OfferId == offerId);
        }

        public async Task<Offer?> GetActiveOfferForProductAsync(long productId)
        {
            var activeOffers = await _context.Offers
                .Where(o => o.ProductId == productId && o.IsActive)
                .ToListAsync();

            if (activeOffers.Count == 0)
                return null;
            
            if (activeOffers.Count == 1)
                return activeOffers.First();

            throw new InvalidOperationException("MULTIPLE_ACTIVE_OFFERS_FOUND");
        }

        public async Task<DateTime?> GetAdoptionMarkerUtcAsync()
        {
            var markerSetting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "offers_usage_adoption_utc");

            if (markerSetting != null && DateTime.TryParse(markerSetting.SettingValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                return parsedDate.ToUniversalTime();
            }

            return null;
        }

        public async Task<List<Product>> ProductsLookupAsync(string? search, int limit)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    p.Barcode.Contains(s));
            }

            return await query
                .OrderBy(p => p.Name)
                .Take(limit)
                .ToListAsync();
        }
    }
}
