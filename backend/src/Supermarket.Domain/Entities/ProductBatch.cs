using System;

namespace Supermarket.Domain.Entities
{
    public class ProductBatch
    {
        public long Id { get; set; }
        
        public long ProductId { get; set; }
        public Product? Product { get; set; }
        
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAvailable { get; set; }
        
        public DateTime? EntryDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EntryInvoiceNumber { get; set; }
        
        public long EnteredByEmployeeId { get; set; }
        public Employee? EnteredByEmployee { get; set; }
    }
}
