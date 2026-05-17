namespace Supermarket.Contracts.InvoicesQuery
{
    public class InvoiceLineDto
    {
        public long LineId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public long? OfferId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPriceUsdOriginal { get; set; }
        public decimal LineTotalUsdOriginal { get; set; }
        public decimal LineTotalUsdEffective { get; set; }
        public bool IsPriceOverridden { get; set; }
        public int SortOrder { get; set; }
    }
}
