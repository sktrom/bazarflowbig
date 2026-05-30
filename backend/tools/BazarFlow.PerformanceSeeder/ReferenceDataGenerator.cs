namespace BazarFlow.PerformanceSeeder;

public static class ReferenceDataGenerator
{
    private static readonly string[] Units = ["piece", "kg", "liter", "pack", "box"];
    private static readonly DateTime BaseCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static ReferenceDataPlan Generate(ProfileConfig profile, int seed)
    {
        var categories = Enumerable.Range(1, profile.Categories)
            .Select(index => new SyntheticCategorySpec(SyntheticPreviewGenerator.CategoryName(seed, index)))
            .ToList();

        var suppliers = Enumerable.Range(1, profile.Suppliers)
            .Select(index =>
            {
                var timestamp = Timestamp(index);
                return new SyntheticSupplierSpec(
                    SyntheticPreviewGenerator.SupplierName(seed, index),
                    $"perf.supplier{index:D3}.{SyntheticPreviewGenerator.SeedToken(seed)}@example.test",
                    $"+000000{index:D5}",
                    $"BF-PERF Synthetic Address {index:D6}",
                    "Synthetic performance supplier",
                    timestamp,
                    timestamp);
            })
            .ToList();

        var products = Enumerable.Range(1, profile.Products)
            .Select(index =>
            {
                var random = CreateRandom(seed, index, 701);
                var cost = Math.Round((decimal)(random.NextDouble() * 45.0 + 1.0), 2);
                var margin = Math.Round((decimal)(random.NextDouble() * 0.55 + 0.10), 2);
                var price = Math.Round(cost * (1 + margin), 2);
                var hasCarton = index % 4 == 0;
                int? cartonQuantity = hasCarton ? 12 + index % 24 : null;
                decimal? cartonPrice = hasCarton ? Math.Round(price * cartonQuantity!.Value * 0.95m, 2) : null;
                var timestamp = Timestamp(index);

                return new SyntheticProductSpec(
                    SyntheticPreviewGenerator.ProductName(seed, index),
                    SyntheticPreviewGenerator.Barcode(seed, index),
                    Units[index % Units.Length],
                    price,
                    hasCarton,
                    cartonQuantity,
                    cartonPrice,
                    index % 3 == 0,
                    timestamp,
                    timestamp,
                    (index - 1) % profile.Categories);
            })
            .ToList();

        var employees = Enumerable.Range(1, profile.Employees)
            .Select(index =>
            {
                var timestamp = Timestamp(index);
                return new SyntheticEmployeeSpec(
                    $"BF-PERF Employee {SyntheticPreviewGenerator.SeedToken(seed)}-{index:D6}",
                    SyntheticPreviewGenerator.EmployeeUsername(seed, index),
                    $"+000100{index:D5}",
                    timestamp,
                    timestamp);
            })
            .ToList();

        var devices = Enumerable.Range(1, profile.Devices)
            .Select(index =>
            {
                var timestamp = Timestamp(index);
                return new SyntheticDeviceSpec(
                    SyntheticPreviewGenerator.DeviceCode(seed, index),
                    SyntheticPreviewGenerator.DeviceName(seed, index),
                    "Synthetic performance device",
                    timestamp,
                    timestamp);
            })
            .ToList();

        return new ReferenceDataPlan(seed, profile, categories, suppliers, products, employees, devices);
    }

    private static DateTime Timestamp(int index)
    {
        return BaseCreatedAt.AddDays(index % 30).AddMinutes(index % 1440);
    }

    private static Random CreateRandom(int seed, int index, int salt)
    {
        var mixedSeed = unchecked((seed * 397) ^ (index * 104729) ^ (salt * 7919));
        return new Random(mixedSeed);
    }
}
