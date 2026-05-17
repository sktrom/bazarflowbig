using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.ProductBatches
{
    public class BatchListItem
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAvailable { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EntryInvoiceNumber { get; set; }
        public long EnteredByEmployeeId { get; set; }
    }

    public class BatchListResponse
    {
        public List<BatchListItem> Items { get; set; } = new();
    }

    public class CreateBatchRequest
    {
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAvailable { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EntryInvoiceNumber { get; set; }
    }

    public class BatchDetailResponse
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAvailable { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EntryInvoiceNumber { get; set; }
        public long EnteredByEmployeeId { get; set; }
    }
}
