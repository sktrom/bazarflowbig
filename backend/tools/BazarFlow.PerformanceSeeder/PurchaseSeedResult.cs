namespace BazarFlow.PerformanceSeeder;

public sealed record PurchaseSeedResult(
    EntitySeedResult Purchases,
    EntitySeedResult PurchaseLines,
    EntitySeedResult ProductBatches);
