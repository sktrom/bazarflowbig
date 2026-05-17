using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class InvoiceLineBatchAllocation
    {
        public long Id { get; set; }
        
        public long InvoiceLineId { get; set; }
        public InvoiceLine? InvoiceLine { get; set; }
        
        public long BatchId { get; set; }
        public ProductBatch? Batch { get; set; }
        
        public decimal Quantity { get; set; }
        public AllocationStatus AllocationStatus { get; set; }
    }
}
