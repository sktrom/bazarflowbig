using System;
using System.Threading.Tasks;
using Supermarket.Contracts.InvoicesQuery;

namespace Supermarket.Application.InvoicesQuery.Interfaces
{
    public interface IInvoicesQueryService
    {
        Task<InvoiceListResponse> GetInvoicesAsync(
            string? status = null,
            long? employeeId = null,
            string? customerName = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool? hasAdjustmentRequest = null,
            string? adjustmentRequestStatus = null,
            bool? manualPriceEdited = null,
            TimeSpan? timeFrom = null,
            TimeSpan? timeTo = null,
            string? sortBy = null,
            string? sortOrder = null,
            int page = 1,
            int pageSize = 20);

        Task<InvoiceSummaryResponse> GetInvoiceSummaryAsync(long invoiceId);
        Task<InvoiceDetailsResponse> GetInvoiceDetailsAsync(long invoiceId);
    }
}
