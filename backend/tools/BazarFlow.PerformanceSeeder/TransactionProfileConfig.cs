namespace BazarFlow.PerformanceSeeder;

public sealed record TransactionProfileConfig(
    string Name,
    int Purchases,
    int MinLinesPerPurchase,
    int MaxLinesPerPurchase,
    int Invoices,
    int MinLinesPerInvoice,
    int MaxLinesPerInvoice,
    int BlackBoxEvents,
    int DateWindowDays)
{
    private static readonly IReadOnlyDictionary<string, TransactionProfileConfig> Profiles =
        new Dictionary<string, TransactionProfileConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["small"] = new("small", 100, 5, 20, 1_000, 1, 20, 2_000, 30),
            ["medium"] = new("medium", 1_000, 5, 20, 10_000, 1, 50, 30_000, 90),
            ["large"] = new("large", 5_000, 5, 20, 50_000, 1, 80, 200_000, 180)
        };

    public static TransactionProfileConfig Get(string profile) => Profiles[profile];

    public int MinimumPurchaseLines => Purchases * MinLinesPerPurchase;

    public int MaximumPurchaseLines => Purchases * MaxLinesPerPurchase;

    public int MinimumInvoiceLines => Invoices * MinLinesPerInvoice;

    public int MaximumInvoiceLines => Invoices * MaxLinesPerInvoice;
}
