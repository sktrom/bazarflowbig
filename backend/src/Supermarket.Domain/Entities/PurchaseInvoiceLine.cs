using System;

namespace Supermarket.Domain.Entities
{
    public class PurchaseInvoiceLine
    {
        public long Id { get; set; }

        public long PurchaseInvoiceId { get; set; }
        public PurchaseInvoice? PurchaseInvoice { get; set; }

        public long ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitCostUsd { get; set; }
        public decimal LineTotalUsd { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
    }
}
