using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Contracts.InvoicesQuery;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.InvoicesQuery.Services
{
    public class InvoicesQueryService : IInvoicesQueryService
    {
        private readonly IInvoicesQueryRepository _repository;

        public InvoicesQueryService(IInvoicesQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<InvoiceListResponse> GetInvoicesAsync(
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
            int pageSize = 20)
        {
            InvoiceStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<InvoiceStatus>(status, out var s))
                    throw new InvalidOperationException("INVALID_STATUS_FILTER");
                statusEnum = s;
            }

            AdjustmentRequestStatus? adjStatusEnum = null;
            if (!string.IsNullOrWhiteSpace(adjustmentRequestStatus))
            {
                if (!Enum.TryParse<AdjustmentRequestStatus>(adjustmentRequestStatus, out var a))
                    throw new InvalidOperationException("INVALID_ADJUSTMENT_STATUS_FILTER");
                adjStatusEnum = a;
            }

            DateTime? finalDateFrom = dateFrom;
            if (dateFrom.HasValue && timeFrom.HasValue)
            {
                finalDateFrom = dateFrom.Value.Date.Add(timeFrom.Value);
            }

            DateTime? finalDateTo = dateTo;
            if (dateTo.HasValue && timeTo.HasValue)
            {
                finalDateTo = dateTo.Value.Date.Add(timeTo.Value);
            }

            var (items, totalCount) = await _repository.GetInvoicesPaginatedAsync(
                statusEnum, employeeId, customerName, finalDateFrom, finalDateTo,
                hasAdjustmentRequest, adjStatusEnum, manualPriceEdited, sortBy, sortOrder, page, pageSize);

            var response = new InvoiceListResponse
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            foreach (var item in items)
            {
                var inv = item.Invoice;
                var latestAdj = item.LatestAdjustment;

                response.Items.Add(new InvoiceListItemDto
                {
                    InvoiceId = inv.Id,
                    InvoiceNumber = inv.InvoiceNumber,
                    Status = inv.Status.ToString(),
                    CustomerName = inv.CustomerName,
                    OriginalEmployeeId = inv.OriginalEmployeeId,
                    EmployeeName = inv.OriginalEmployee?.FullName,
                    CreatedAt = inv.CreatedAt,
                    CompletedAt = inv.CompletedAt,
                    TotalUsd = inv.TotalUsd,
                    TotalSyp = inv.TotalSyp,
                    HasManualPriceEdit = inv.HasManualPriceEdit,
                    HasAdjustmentRequest = inv.HasAdjustmentRequest,
                    AdjustmentRequestStatus = latestAdj?.Status.ToString(),
                    AdjustmentRequestId = latestAdj?.Id,
                    SuspensionReason = inv.SuspensionReason?.ToString()
                });
            }

            return response;
        }

        public async Task<InvoiceSummaryResponse> GetInvoiceSummaryAsync(long invoiceId)
        {
            var (invoice, latestAdj) = await _repository.GetInvoiceSummaryByIdAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("INVOICE_NOT_FOUND");

            return new InvoiceSummaryResponse
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Status = invoice.Status.ToString(),
                CustomerName = invoice.CustomerName,
                OriginalEmployeeId = invoice.OriginalEmployeeId,
                EmployeeName = invoice.OriginalEmployee?.FullName,
                InvoiceDiscountType = invoice.InvoiceDiscountType?.ToString(),
                InvoiceDiscountValue = invoice.InvoiceDiscountValue,
                SubtotalUsd = invoice.SubtotalUsd,
                TotalUsd = invoice.TotalUsd,
                ExchangeRateSypSnapshot = invoice.ExchangeRateSypSnapshot,
                TotalSyp = invoice.TotalSyp,
                HasManualPriceEdit = invoice.HasManualPriceEdit,
                HasAdjustmentRequest = invoice.HasAdjustmentRequest,
                AdjustmentRequestStatus = latestAdj?.Status.ToString(),
                AdjustmentRequestId = latestAdj?.Id,
                CreatedAt = invoice.CreatedAt,
                CompletedAt = invoice.CompletedAt,
                SuspensionReason = invoice.SuspensionReason?.ToString()
            };
        }

        public async Task<InvoiceDetailsResponse> GetInvoiceDetailsAsync(long invoiceId)
        {
            var (invoice, lines, latestAdj) = await _repository.GetInvoiceDetailsByIdAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("INVOICE_NOT_FOUND");

            var response = new InvoiceDetailsResponse
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Status = invoice.Status.ToString(),
                CustomerName = invoice.CustomerName,
                OriginalEmployeeId = invoice.OriginalEmployeeId,
                EmployeeName = invoice.OriginalEmployee?.FullName,
                InvoiceDiscountType = invoice.InvoiceDiscountType?.ToString(),
                InvoiceDiscountValue = invoice.InvoiceDiscountValue,
                SubtotalUsd = invoice.SubtotalUsd,
                TotalUsd = invoice.TotalUsd,
                ExchangeRateSypSnapshot = invoice.ExchangeRateSypSnapshot,
                TotalSyp = invoice.TotalSyp,
                HasManualPriceEdit = invoice.HasManualPriceEdit,
                HasAdjustmentRequest = invoice.HasAdjustmentRequest,
                AdjustmentRequestStatus = latestAdj?.Status.ToString(),
                AdjustmentRequestType = latestAdj?.RequestType.ToString(),
                AdjustmentRequestId = latestAdj?.Id,
                CreatedAt = invoice.CreatedAt,
                CompletedAt = invoice.CompletedAt,
                SuspensionReason = invoice.SuspensionReason?.ToString()
            };

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    response.Lines.Add(new InvoiceLineDto
                    {
                        LineId = line.Id,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name ?? string.Empty,
                        OfferId = line.OfferId,
                        Quantity = line.Quantity,
                        UnitPriceUsdOriginal = line.UnitPriceUsdOriginal,
                        LineTotalUsdOriginal = line.LineTotalUsdOriginal,
                        LineTotalUsdEffective = line.LineTotalUsdEffective,
                        IsPriceOverridden = line.IsPriceOverridden,
                        SortOrder = line.SortOrder
                    });
                }
            }

            return response;
        }
    }
}
