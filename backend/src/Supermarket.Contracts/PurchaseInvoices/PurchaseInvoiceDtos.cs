using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.PurchaseInvoices
{
    public class PurchaseInvoiceListItem
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ExternalInvoiceNumber { get; set; }
        public decimal SubtotalUsd { get; set; }
        public decimal TotalUsd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseInvoiceListResponse
    {
        public List<PurchaseInvoiceListItem> Items { get; set; } = new();
    }

    public class PurchaseInvoiceDetailResponse
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public long CreatedByEmployeeId { get; set; }
        public string CreatedByEmployeeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ExternalInvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public decimal SubtotalUsd { get; set; }
        public decimal TotalUsd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PurchaseInvoiceLineDto> Lines { get; set; } = new();
    }

    public class PurchaseInvoiceLineDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCostUsd { get; set; }
        public decimal LineTotalUsd { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreatePurchaseInvoiceRequest
    {
        public long SupplierId { get; set; }
        public string? ExternalInvoiceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePurchaseInvoiceRequest
    {
        public long SupplierId { get; set; }
        public string? ExternalInvoiceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class CreatePurchaseInvoiceLineRequest
    {
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCostUsd { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePurchaseInvoiceLineRequest
    {
        public decimal Quantity { get; set; }
        public decimal UnitCostUsd { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class DeletePurchaseInvoiceResponse
    {
        public bool Success { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class DeletePurchaseInvoiceLineResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PurchaseProductLookupItem
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public bool HasExpiry { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
    }

    public class PurchaseProductLookupResponse
    {
        public List<PurchaseProductLookupItem> Items { get; set; } = new();
    }
}
