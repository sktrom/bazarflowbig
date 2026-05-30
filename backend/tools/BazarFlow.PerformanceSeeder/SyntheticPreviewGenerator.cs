namespace BazarFlow.PerformanceSeeder;

public static class SyntheticPreviewGenerator
{
    public const int DefaultSeed = 12345;

    public static string Barcode(int seed, int index)
    {
        var random = CreateRandom(seed, index, 101);
        var seedPart = Math.Abs(seed % 100000);
        return $"BF-PERF-{seedPart:D5}-{index:D6}-{random.Next(1000, 9999)}";
    }

    public static string ProductName(int seed, int index)
    {
        var random = CreateRandom(seed, index, 202);
        var adjectives = new[] { "Basic", "Fresh", "Prime", "Daily", "Value", "Select" };
        var nouns = new[] { "Rice", "Juice", "Soap", "Pasta", "Tea", "Beans" };
        return $"Synthetic {adjectives[random.Next(adjectives.Length)]} {nouns[random.Next(nouns.Length)]} {index:D6}";
    }

    public static string EmployeeUsername(int seed, int index)
    {
        var random = CreateRandom(seed, index, 303);
        return $"perf.employee{index:D3}.{random.Next(1000, 9999)}@example.test";
    }

    private static Random CreateRandom(int seed, int index, int salt)
    {
        var mixedSeed = unchecked((seed * 397) ^ (index * 104729) ^ (salt * 7919));
        return new Random(mixedSeed);
    }
}
