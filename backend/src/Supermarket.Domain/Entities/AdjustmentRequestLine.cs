using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class AdjustmentRequestLine
    {
        public long Id { get; set; }
        
        public long AdjustmentRequestId { get; set; }
        public AdjustmentRequest? AdjustmentRequest { get; set; }
        
        public long? InvoiceLineId { get; set; }
        public InvoiceLine? InvoiceLine { get; set; }
        
        public AdjustmentLineActionType ActionType { get; set; }
        
        public decimal? RequestedQuantity { get; set; }
        public decimal? RequestedLineTotalUsd { get; set; }
    }
}
