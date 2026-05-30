namespace BazarFlow.PerformanceSeeder;

public sealed record ProfileConfig(
    string Name,
    int Categories,
    int Suppliers,
    int Products,
    int Employees,
    int Devices,
    int Invoices)
{
    private static readonly IReadOnlyDictionary<string, ProfileConfig> Profiles =
        new Dictionary<string, ProfileConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["small"] = new("small", 10, 5, 500, 3, 3, 1_000),
            ["medium"] = new("medium", 50, 30, 5_000, 10, 10, 10_000),
            ["large"] = new("large", 150, 100, 20_000, 30, 30, 50_000)
        };

    public static bool IsSupported(string? profile) =>
        !string.IsNullOrWhiteSpace(profile) && Profiles.ContainsKey(profile);

    public static ProfileConfig Get(string profile) => Profiles[profile];
}
