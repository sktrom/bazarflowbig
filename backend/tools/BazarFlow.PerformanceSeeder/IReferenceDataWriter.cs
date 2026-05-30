namespace BazarFlow.PerformanceSeeder;

public interface IReferenceDataWriter : IAsyncDisposable
{
    Task<ReferenceDataSeedResult> SeedAsync(ReferenceDataPlan plan, TextWriter output, CancellationToken cancellationToken = default);

    Task<PurchaseSeedResult> SeedPurchasesAsync(int seed, ProfileConfig profile, TextWriter output, CancellationToken cancellationToken = default);

    Task<InvoiceSeedResult> SeedInvoicesAsync(int seed, ProfileConfig profile, TextWriter output, CancellationToken cancellationToken = default);
}
