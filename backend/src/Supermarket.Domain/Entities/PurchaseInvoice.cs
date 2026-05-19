using System;
using System.Collections.Generic;
using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class PurchaseInvoice
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public long SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public long CreatedByEmployeeId { get; set; }
        public Employee? CreatedByEmployee { get; set; }

        public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Draft;
        public string? ExternalInvoiceNumber { get; set; }
        public string? Notes { get; set; }

        public decimal SubtotalUsd { get; set; }
        public decimal TotalUsd { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<PurchaseInvoiceLine> Lines { get; set; } = new();
    }
}
