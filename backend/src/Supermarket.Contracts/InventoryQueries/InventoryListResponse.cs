using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.InventoryQueries
{
    public class InventoryListResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<InventoryListItemDto> Items { get; set; } = new();
    }

    public class InventoryListItemDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public bool HasCarton { get; set; }
        public int? CartonQuantity { get; set; }
        public decimal? CartonPriceUsd { get; set; }
        public bool HasExpiry { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalQuantityAvailable { get; set; }
        public int BatchCount { get; set; }
        public DateTime? NearestExpiryDate { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string? ExpiryStatus { get; set; }
    }
}
