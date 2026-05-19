using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Offers.Interfaces
{
    public interface IOfferManagementRepository
    {
        Task<IReadOnlyList<Offer>> GetAllAsync();
        Task<Offer?> GetByIdAsync(long id);
        Task<Offer> CreateAsync(Offer offer);
        Task UpdateAsync(Offer offer);
        Task DeleteAsync(long id);
        
        Task<bool> ProductExistsAsync(long productId);
        Task<bool> HasInvoiceLinesAsync(long offerId);
        
        // Internal lookup tool for Cache/Pricing validation
        Task<Offer?> GetActiveOfferForProductAsync(long productId);
        
        Task<DateTime?> GetAdoptionMarkerUtcAsync();

        // Products lookup scoped to Offers permission
        Task<List<Product>> ProductsLookupAsync(string? search, int limit);
    }
}
