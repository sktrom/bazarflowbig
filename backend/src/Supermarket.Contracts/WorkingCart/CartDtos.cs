using System.Collections.Generic;

namespace Supermarket.Contracts.WorkingCart
{
    public class CartResponse
    {
        public long? InvoiceId { get; set; }
        public string Status { get; set; } = "Working";
        public string? CustomerName { get; set; }
        public string? InvoiceDiscountType { get; set; }
        public decimal? InvoiceDiscountValue { get; set; }
        public decimal SubtotalUsd { get; set; }
        public decimal TotalUsd { get; set; }
        public List<CartLineDto> Lines { get; set; } = new();
    }

    public class CartLineDto
    {
        public long LineId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPriceUsdOriginal { get; set; }
        public decimal LineTotalUsdOriginal { get; set; }
        public decimal LineTotalUsdEffective { get; set; }
        public bool IsPriceOverridden { get; set; }
        public long? OfferId { get; set; }
    }

    public class AddByBarcodeRequest
    {
        public string Barcode { get; set; } = string.Empty;
    }

    public class AddByProductRequest
    {
        public long ProductId { get; set; }
    }

    public class UpdateLineRequest
    {
        public decimal? Quantity { get; set; }
        public decimal? OverrideLineTotalUsd { get; set; }
    }

    public class UpdateDiscountRequest
    {
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public string CustomerName { get; set; } = string.Empty;
    }
}
