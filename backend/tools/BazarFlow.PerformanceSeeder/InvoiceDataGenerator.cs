using Supermarket.Domain.Enums;

namespace BazarFlow.PerformanceSeeder;

public static class InvoiceDataGenerator
{
    private static readonly DateTime AnchorUtc = new(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);

    public static InvoicePlan Generate(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticProductRef> products,
        IReadOnlyList<SyntheticEmployeeRef> employees,
        int startIndex = 1,
        int? count = null)
    {
        if (products.Count == 0 || employees.Count == 0)
        {
            throw new InvalidOperationException("Run core reference data generation first.");
        }

        var invoiceCount = count ?? profile.Invoices;
        var invoices = new List<SyntheticInvoiceSpec>(invoiceCount);
        for (var invoiceIndex = startIndex; invoiceIndex < startIndex + invoiceCount; invoiceIndex++)
        {
            var random = CreateRandom(seed, invoiceIndex, 1_501);
            var lineCount = random.Next(profile.MinLinesPerInvoice, profile.MaxLinesPerInvoice + 1);
            var createdAt = CreateDate(seed, invoiceIndex, profile.DateWindowDays);
            var status = CreateStatus(invoiceIndex);
            var employee = employees[(invoiceIndex - 1) % employees.Count];
            var lines = new List<SyntheticInvoiceLineSpec>(lineCount);

            for (var lineIndex = 1; lineIndex <= lineCount; lineIndex++)
            {
                var lineRandom = CreateRandom(seed, invoiceIndex, 1_700 + lineIndex);
                var product = products[(invoiceIndex * 19 + lineIndex * 37) % products.Count];
                var quantity = lineRandom.Next(1, 11);
                var unitPrice = product.PriceUsd > 0 ? product.PriceUsd : Math.Round(1m + (invoiceIndex % 50) + (lineIndex * 0.25m), 2);
                var lineTotal = Math.Round(quantity * unitPrice, 2);

                lines.Add(new SyntheticInvoiceLineSpec(
                    product.Id,
                    product.Barcode,
                    quantity,
                    unitPrice,
                    lineTotal,
                    lineTotal,
                    lineIndex));
            }

            invoices.Add(new SyntheticInvoiceSpec(
                InvoiceNumber(seed, invoiceIndex),
                employee.Id,
                invoiceIndex % 4 == 0 ? null : $"BF-PERF Customer {invoiceIndex:D6}",
                status,
                createdAt,
                createdAt.AddMinutes(random.Next(1, 31)),
                status == InvoiceStatus.Modified && invoiceIndex % 2 == 0,
                lines));
        }

        return new InvoicePlan(seed, profile, invoices);
    }

    public static string InvoiceNumber(int seed, int index) =>
        $"BF-PERF-INV-{SyntheticPreviewGenerator.SeedToken(seed)}-{index:D6}";

    private static InvoiceStatus CreateStatus(int invoiceIndex)
    {
        var bucket = invoiceIndex % 100;
        if (bucket < 95)
        {
            return InvoiceStatus.Completed;
        }

        return bucket < 98 ? InvoiceStatus.Modified : InvoiceStatus.Cancelled;
    }

    private static DateTime CreateDate(int seed, int index, int dateWindowDays)
    {
        var random = CreateRandom(seed, index, 1_502);
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
