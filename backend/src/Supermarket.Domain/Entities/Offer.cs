using System;
using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class Offer
    {
        public long Id { get; set; }
        
        public long ProductId { get; set; }
        public Product? Product { get; set; }
        
        public OfferDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
