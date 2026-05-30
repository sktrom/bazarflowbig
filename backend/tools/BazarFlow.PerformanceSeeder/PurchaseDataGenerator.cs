namespace BazarFlow.PerformanceSeeder;

public static class PurchaseDataGenerator
{
    private static readonly DateTime AnchorUtc = new(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);

    public static PurchasePlan Generate(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticProductRef> products,
        IReadOnlyList<SyntheticSupplierRef> suppliers,
        IReadOnlyList<SyntheticEmployeeRef> employees)
    {
        if (products.Count == 0 || suppliers.Count == 0 || employees.Count == 0)
        {
            throw new InvalidOperationException("Run core reference data generation first.");
        }

        var purchases = new List<SyntheticPurchaseSpec>(profile.Purchases);
        for (var purchaseIndex = 1; purchaseIndex <= profile.Purchases; purchaseIndex++)
        {
            var random = CreateRandom(seed, purchaseIndex, 901);
            var lineCount = random.Next(profile.MinLinesPerPurchase, profile.MaxLinesPerPurchase + 1);
            var createdAt = CreateDate(seed, purchaseIndex, profile.DateWindowDays);
            var supplier = suppliers[(purchaseIndex - 1) % suppliers.Count];
            var createdBy = employees[(purchaseIndex - 1) % employees.Count];
            var completedBy = employees[purchaseIndex % employees.Count];
            var lines = new List<SyntheticPurchaseLineSpec>(lineCount);

            for (var lineIndex = 1; lineIndex <= lineCount; lineIndex++)
            {
                var lineRandom = CreateRandom(seed, purchaseIndex, 1_000 + lineIndex);
                var product = products[(purchaseIndex * 17 + lineIndex * 31) % products.Count];
                var quantity = lineRandom.Next(10, 501);
                var productPrice = product.PriceUsd > 0 ? product.PriceUsd : 1m;
                var costFactor = (decimal)(0.45 + lineRandom.NextDouble() * 0.35);
                var unitCost = Math.Max(0.01m, Math.Round(productPrice * costFactor, 2));
                if (unitCost >= productPrice)
                {
                    unitCost = Math.Max(0.01m, Math.Round(productPrice * 0.80m, 2));
                }

                var lineTotal = Math.Round(quantity * unitCost, 2);
                var expiryDate = product.HasExpiry ? createdAt.AddDays(90 + lineRandom.Next(0, 365)) : (DateTime?)null;

                lines.Add(new SyntheticPurchaseLineSpec(
                    product.Id,
                    product.Barcode,
                    quantity,
                    unitCost,
                    lineTotal,
                    expiryDate,
                    lineIndex));
            }

            purchases.Add(new SyntheticPurchaseSpec(
                PurchaseInvoiceNumber(seed, purchaseIndex),
                PurchaseExternalInvoiceNumber(seed, purchaseIndex),
                supplier.Id,
                createdBy.Id,
                completedBy.Id,
                createdAt,
                createdAt,
                createdAt.AddMinutes(random.Next(5, 180)),
                lines));
        }

        return new PurchasePlan(seed, profile, purchases);
    }

    public static string PurchaseInvoiceNumber(int seed, int index) =>
        $"BF-PERF-PUR-{SyntheticPreviewGenerator.SeedToken(seed)}-{index:D6}";

    public static string PurchaseExternalInvoiceNumber(int seed, int index) =>
        $"BF-PERF-EXT-PUR-{SyntheticPreviewGenerator.SeedToken(seed)}-{index:D6}";

    private static DateTime CreateDate(int seed, int index, int dateWindowDays)
    {
        var random = CreateRandom(seed, index, 902);
        return AnchorUtc
            .AddDays(-random.Next(0, dateWindowDays))
            .AddHours(random.Next(0, 24))
            .AddMinutes(random.Next(0, 60));
    }

    private static Random CreateRandom(int seed, int index, int salt)
    {
        var mixedSeed = unchecked((seed * 397) ^ (index * 104729) ^ (salt * 7919));
        return new Random(mixedSeed);
    }
}
