namespace BazarFlow.PerformanceSeeder;

public static class SyntheticPreviewGenerator
{
    public const int DefaultSeed = 12345;
    public const string TestOnlyPassword = "PerformanceTest!123";

    public static string Barcode(int seed, int index)
    {
        return $"BF-PERF-{SeedToken(seed)}-{index:D6}";
    }

    public static string ProductName(int seed, int index)
    {
        return $"BF-PERF Product {SeedToken(seed)}-{index:D6}";
    }

    public static string EmployeeUsername(int seed, int index)
    {
        return $"perf.employee{index:D3}.{SeedToken(seed)}@example.test";
    }

    public static string CategoryName(int seed, int index)
    {
        return $"BF-PERF Category {SeedToken(seed)}-{index:D6}";
    }

    public static string SupplierName(int seed, int index)
    {
        return $"BF-PERF Supplier {SeedToken(seed)}-{index:D6}";
    }

    public static string DeviceCode(int seed, int index)
    {
        return $"BF-PERF-DEV-{SeedToken(seed)}-{index:D6}";
    }

    public static string DeviceName(int seed, int index)
    {
        return $"BF-PERF Device {SeedToken(seed)}-{index:D6}";
    }

    public static string SeedToken(int seed)
    {
        return seed.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace("-", "N", StringComparison.Ordinal);
    }
}
