using System.Threading.Tasks;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Invoices.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.Invoices.Services
{
    public class InvoiceAppService : IInvoiceAppService
    {
        private readonly IInvoiceManagementRepository _invoiceRepo;
        private readonly IOfferManagementRepository _offerRepo;

        public InvoiceAppService(IInvoiceManagementRepository invoiceRepo, IOfferManagementRepository offerRepo)
        {
            _invoiceRepo = invoiceRepo;
            _offerRepo = offerRepo;
        }

        public async Task CreateInvoiceLineMinimalAsync(long invoiceId, long productId, decimal quantity, decimal unitPriceUsdOriginal)
        {
            var line = new InvoiceLine
            {
                InvoiceId = invoiceId,
                ProductId = productId,
                Quantity = quantity,
                UnitPriceUsdOriginal = unitPriceUsdOriginal,
                LineTotalUsdOriginal = quantity * unitPriceUsdOriginal,
                IsPriceOverridden = false,
                SortOrder = 1
            };

            var activeOffer = await _offerRepo.GetActiveOfferForProductAsync(productId);
            
            if (activeOffer != null)
            {
                // Offer applied actually
                line.OfferId = activeOffer.Id;
                
                if (activeOffer.DiscountType == OfferDiscountType.Amount)
                {
                    line.LineTotalUsdEffective = line.LineTotalUsdOriginal - activeOffer.DiscountValue;
                }
                else if (activeOffer.DiscountType == OfferDiscountType.Percent)
                {
                    var discountAmount = line.LineTotalUsdOriginal * (activeOffer.DiscountValue / 100m);
                    line.LineTotalUsdEffective = line.LineTotalUsdOriginal - discountAmount;
                }
            }
            else
            {
                // No active offer, or ambiguous ones which might throw before here if fail-closed is strictly throwing
                line.OfferId = null;
                line.LineTotalUsdEffective = line.LineTotalUsdOriginal;
            }

            await _invoiceRepo.CreateInvoiceLineMinimalAsync(line);
        }
    }
}
