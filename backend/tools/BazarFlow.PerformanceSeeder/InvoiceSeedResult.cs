namespace BazarFlow.PerformanceSeeder;

public sealed record InvoiceSeedResult(
    EntitySeedResult Invoices,
    EntitySeedResult InvoiceLines);
