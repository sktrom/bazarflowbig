namespace BazarFlow.PerformanceSeeder;

public sealed record SyntheticProductRef(long Id, string Barcode, decimal PriceUsd, bool HasExpiry);

public sealed record SyntheticSupplierRef(long Id, string Name);

public sealed record SyntheticEmployeeRef(long Id, string Username);

public sealed record PurchasePlan(
    int Seed,
    TransactionProfileConfig Profile,
    IReadOnlyList<SyntheticPurchaseSpec> Purchases)
{
    public int PurchaseLineCount => Purchases.Sum(purchase => purchase.Lines.Count);
}

public sealed record SyntheticPurchaseSpec(
    string InvoiceNumber,
    string ExternalInvoiceNumber,
    long SupplierId,
    long CreatedByEmployeeId,
    long CompletedByEmployeeId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime CompletedAt,
    IReadOnlyList<SyntheticPurchaseLineSpec> Lines)
{
    public decimal SubtotalUsd => Lines.Sum(line => line.LineTotalUsd);

    public decimal TotalUsd => SubtotalUsd;
}

public sealed record SyntheticPurchaseLineSpec(
    long ProductId,
    string ProductBarcode,
    decimal Quantity,
    decimal UnitCostUsd,
    decimal LineTotalUsd,
    DateTime? ExpiryDate,
    int SortOrder);
