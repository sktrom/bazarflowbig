namespace BazarFlow.PerformanceSeeder;

public interface IReferenceDataWriter : IAsyncDisposable
{
    Task<ReferenceDataSeedResult> SeedAsync(ReferenceDataPlan plan, TextWriter output, CancellationToken cancellationToken = default);
}
