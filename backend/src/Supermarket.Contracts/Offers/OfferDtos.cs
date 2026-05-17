using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Offers
{
    public class OfferListItem
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; }
    }

    public class OfferListResponse
    {
        public List<OfferListItem> Items { get; set; } = new();
    }

    public class CreateOfferRequest
    {
        public long ProductId { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
    }

    public class UpdateOfferRequest
    {
        public long ProductId { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
    }

    public class OfferDetailResponse
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CancelOfferResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteOfferResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
