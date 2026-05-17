namespace Supermarket.Domain.Entities
{
    public class InvoiceLine
    {
        public long Id { get; set; }
        
        public long InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
        
        public long ProductId { get; set; }
        public Product? Product { get; set; }
        
        public long? OfferId { get; set; }
        public Offer? Offer { get; set; }
        
        public decimal Quantity { get; set; }
        
        public decimal UnitPriceUsdOriginal { get; set; }
        public decimal LineTotalUsdOriginal { get; set; }
        public decimal LineTotalUsdEffective { get; set; }
        
        public bool IsPriceOverridden { get; set; }
        public int SortOrder { get; set; }
    }
}
