using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.InvoicesQuery.Interfaces
{
    public interface IInvoicesQueryRepository
    {
        Task<(List<(Invoice Invoice, AdjustmentRequest? LatestAdjustment)> Items, int TotalCount)> GetInvoicesPaginatedAsync(
            InvoiceStatus? status,
            long? employeeId,
            string? customerName,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? hasAdjustmentRequest,
            AdjustmentRequestStatus? adjustmentRequestStatus,
            bool? manualPriceEdited,
            string? sortBy,
            string? sortOrder,
            int page,
            int pageSize);

        Task<(Invoice? Invoice, AdjustmentRequest? LatestAdjustment)> GetInvoiceSummaryByIdAsync(long invoiceId);
        Task<(Invoice? Invoice, List<InvoiceLine> Lines, AdjustmentRequest? LatestAdjustment)> GetInvoiceDetailsByIdAsync(long invoiceId);
    }
}
