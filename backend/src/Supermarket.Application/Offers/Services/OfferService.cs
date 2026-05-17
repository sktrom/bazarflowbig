using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Contracts.Offers;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.Offers.Services
{
    public class OfferService : IOfferService
    {
        private readonly IOfferManagementRepository _repository;

        public OfferService(IOfferManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task<OfferListResponse> GetAllAsync()
        {
            var offers = await _repository.GetAllAsync();
            return new OfferListResponse
            {
                Items = offers.Select(o => new OfferListItem
                {
                    Id = o.Id,
                    ProductId = o.ProductId,
                    ProductName = o.Product?.Name ?? string.Empty,
                    DiscountType = o.DiscountType.ToString(),
                    DiscountValue = o.DiscountValue,
                    IsActive = o.IsActive
                }).ToList()
            };
        }

        public async Task<OfferDetailResponse> CreateAsync(CreateOfferRequest request)
        {
            if (!Enum.TryParse<OfferDiscountType>(request.DiscountType, out var discountType))
                throw new InvalidOperationException("INVALID_DISCOUNT_TYPE");

            if (request.DiscountValue <= 0)
                throw new InvalidOperationException("VALIDATION_ERROR");

            if (!await _repository.ProductExistsAsync(request.ProductId))
                throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            var offer = new Offer
            {
                ProductId = request.ProductId,
                DiscountType = discountType,
                DiscountValue = request.DiscountValue,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(offer);
            var fetched = await _repository.GetByIdAsync(created.Id);

            return MapToDetailResponse(fetched ?? created);
        }

        public async Task<OfferDetailResponse> UpdateAsync(long id, UpdateOfferRequest request)
        {
            if (!Enum.TryParse<OfferDiscountType>(request.DiscountType, out var discountType))
                throw new InvalidOperationException("INVALID_DISCOUNT_TYPE");

            if (request.DiscountValue <= 0)
                throw new InvalidOperationException("VALIDATION_ERROR");

            if (!await _repository.ProductExistsAsync(request.ProductId))
                throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            var offer = await _repository.GetByIdAsync(id);
            if (offer == null) throw new InvalidOperationException("OFFER_NOT_FOUND");

            offer.ProductId = request.ProductId;
            offer.DiscountType = discountType;
            offer.DiscountValue = request.DiscountValue;
            // NOTE: IsActive is purposefully omitted from updates.
            offer.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(offer);
            
            var fetched = await _repository.GetByIdAsync(offer.Id);
            return MapToDetailResponse(fetched ?? offer);
        }

        public async Task<CancelOfferResponse> CancelAsync(long id)
        {
            var offer = await _repository.GetByIdAsync(id);
            if (offer == null) throw new InvalidOperationException("OFFER_NOT_FOUND");

            offer.IsActive = false;
            offer.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(offer);

            return new CancelOfferResponse 
            { 
                Success = true, 
                Message = "Offer cancelled/disabled successfully." 
            };
        }

        public async Task<DeleteOfferResponse> DeleteAsync(long id)
        {
            var offer = await _repository.GetByIdAsync(id);
            if (offer == null) throw new InvalidOperationException("OFFER_NOT_FOUND");

            var adoptionMarker = await _repository.GetAdoptionMarkerUtcAsync();
            if (adoptionMarker == null)
            {
                throw new InvalidOperationException("MISSING_ADOPTION_MARKER_CONFIGURATION");
            }

            if (offer.CreatedAt < adoptionMarker)
            {
                // This is a Legacy offer, physical delete is forbidden
                throw new InvalidOperationException("CANNOT_DELETE_LEGACY_OFFER");
            }

            // Provable offer, check usage
            if (await _repository.HasInvoiceLinesAsync(id))
            {
                throw new InvalidOperationException("CANNOT_DELETE_USED_OFFER");
            }

            // Provable and Unused
            await _repository.DeleteAsync(id);

            return new DeleteOfferResponse 
            { 
                Success = true, 
                Message = "Offer deleted successfully." 
            };
        }

        private static OfferDetailResponse MapToDetailResponse(Offer o)
        {
            return new OfferDetailResponse
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product?.Name ?? string.Empty,
                DiscountType = o.DiscountType.ToString(),
                DiscountValue = o.DiscountValue,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            };
        }
    }
}
