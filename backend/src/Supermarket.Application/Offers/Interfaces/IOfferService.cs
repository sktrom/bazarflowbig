using System.Threading.Tasks;
using Supermarket.Contracts.Offers;

namespace Supermarket.Application.Offers.Interfaces
{
    public interface IOfferService
    {
        Task<OfferListResponse> GetAllAsync();
        Task<OfferDetailResponse> CreateAsync(CreateOfferRequest request);
        Task<OfferDetailResponse> UpdateAsync(long id, UpdateOfferRequest request);
        Task<CancelOfferResponse> CancelAsync(long id);
        Task<DeleteOfferResponse> DeleteAsync(long id);
        Task<OfferProductLookupResponse> ProductsLookupAsync(string? search);
    }
}
