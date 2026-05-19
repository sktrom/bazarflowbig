using System.Threading.Tasks;
using Supermarket.Contracts.PurchaseInvoices;

namespace Supermarket.Application.PurchaseInvoices.Interfaces
{
    public interface IPurchaseInvoiceService
    {
        Task<PurchaseInvoiceListResponse> GetAllAsync();
        Task<PurchaseInvoiceDetailResponse> GetByIdAsync(long id);
        Task<PurchaseInvoiceDetailResponse> CreateAsync(CreatePurchaseInvoiceRequest request);
        Task<PurchaseInvoiceDetailResponse> UpdateAsync(long id, UpdatePurchaseInvoiceRequest request);
        Task<DeletePurchaseInvoiceResponse> DeleteAsync(long id);
        Task<PurchaseInvoiceDetailResponse> AddLineAsync(long invoiceId, CreatePurchaseInvoiceLineRequest request);
        Task<PurchaseInvoiceDetailResponse> UpdateLineAsync(long invoiceId, long lineId, UpdatePurchaseInvoiceLineRequest request);
        Task<DeletePurchaseInvoiceLineResponse> DeleteLineAsync(long invoiceId, long lineId);
        Task<PurchaseProductLookupResponse> LookupProductsAsync(string? search);
    }
}
