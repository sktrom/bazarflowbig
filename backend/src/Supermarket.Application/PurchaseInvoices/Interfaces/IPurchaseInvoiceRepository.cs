using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.PurchaseInvoices.Interfaces
{
    public interface IPurchaseInvoiceRepository
    {
        Task<IReadOnlyList<PurchaseInvoice>> GetAllAsync();
        Task<PurchaseInvoice?> GetByIdWithDetailsAsync(long id);
        Task<PurchaseInvoice?> GetByIdForUpdateAsync(long id);
        Task<PurchaseInvoice> CreateAsync(PurchaseInvoice invoice);
        Task UpdateAsync(PurchaseInvoice invoice);
        Task DeleteAsync(PurchaseInvoice invoice);
        Task DeleteLinesAsync(long purchaseInvoiceId);
        Task<PurchaseInvoiceLine> AddLineAsync(PurchaseInvoiceLine line);
        Task<PurchaseInvoiceLine?> GetLineAsync(long purchaseInvoiceId, long lineId);
        Task DeleteLineAsync(PurchaseInvoiceLine line);
        Task<Supplier?> GetSupplierAsync(long supplierId);
        Task<Product?> GetProductAsync(long productId);
        Task<IReadOnlyList<Product>> LookupProductsAsync(string? search, int limit);
        Task<int> GetInvoiceCountForDateAsync(DateTime dateUtc);
        Task<int> GetNextLineSortOrderAsync(long purchaseInvoiceId);
        Task RecalculateTotalsAsync(long purchaseInvoiceId);
        Task ExecuteInTransactionAsync(Func<Task> operation);
        Task SaveChangesAsync();
    }
}
