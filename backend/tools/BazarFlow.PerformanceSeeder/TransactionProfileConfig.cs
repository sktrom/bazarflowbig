namespace BazarFlow.PerformanceSeeder;

public sealed record TransactionProfileConfig(
    string Name,
    int Purchases,
    int MinLinesPerPurchase,
    int MaxLinesPerPurchase,
    int DateWindowDays)
{
    private static readonly IReadOnlyDictionary<string, TransactionProfileConfig> Profiles =
        new Dictionary<string, TransactionProfileConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["small"] = new("small", 100, 5, 20, 30),
            ["medium"] = new("medium", 1_000, 5, 20, 90),
            ["large"] = new("large", 5_000, 5, 20, 180)
        };

    public static TransactionProfileConfig Get(string profile) => Profiles[profile];

    public int MinimumPurchaseLines => Purchases * MinLinesPerPurchase;

    public int MaximumPurchaseLines => Purchases * MaxLinesPerPurchase;
}
