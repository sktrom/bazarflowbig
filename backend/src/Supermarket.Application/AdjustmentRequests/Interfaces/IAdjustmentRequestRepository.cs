using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.AdjustmentRequests.Interfaces
{
    public interface IAdjustmentRequestRepository
    {
        Task<Invoice?> GetInvoiceByIdAsync(long invoiceId);
        Task<bool> HasPendingRequestAsync(long invoiceId);
        Task<bool> HasRejectedRequestAsync(long invoiceId);
        Task<AdjustmentRequest> CreateRequestAsync(AdjustmentRequest request, List<AdjustmentRequestLine> lines);
        Task<AdjustmentRequest?> GetRequestByIdAsync(long requestId);
        Task<List<AdjustmentRequestLine>> GetRequestLinesAsync(long requestId);
        Task<List<InvoiceLine>> GetInvoiceLinesAsync(long invoiceId);
        Task UpdateRequestAsync(AdjustmentRequest request);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceLineAsync(InvoiceLine invoiceLine);
        Task UpdateInvoiceLinesAsync(List<InvoiceLine> invoiceLines);
        Task ReleaseAllInvoiceAllocationsAsync(long invoiceId);
        Task ReleaseInvoiceLineAllocationsAsync(long invoiceLineId);
        Task PartiallyReleaseInvoiceLineAllocationsLifoAsync(long invoiceLineId, decimal quantityToRelease);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
