using System;

namespace Supermarket.Domain.Entities
{
    public class Product
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        
        public long CategoryId { get; set; }
        public Category? Category { get; set; }
        
        public string BaseUnit { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        
        public bool HasCarton { get; set; }
        public int? CartonQuantity { get; set; }
        public decimal? CartonPriceUsd { get; set; }
        
        public bool HasExpiry { get; set; }
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
