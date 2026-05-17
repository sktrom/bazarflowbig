using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.InventoryQueries
{
    public class InventoryDetailsResponse
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
        public string StockStatus { get; set; } = string.Empty;
        public string? ExpiryStatus { get; set; }
        public List<InventoryBatchDto> Batches { get; set; } = new();
    }

    public class InventoryBatchDto
    {
        public long BatchId { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAvailable { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EntryInvoiceNumber { get; set; }
        public long EnteredByEmployeeId { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public string? ExpiryStatus { get; set; }
    }
}
