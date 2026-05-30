using Supermarket.Domain.Enums;

namespace BazarFlow.PerformanceSeeder;

public sealed record InvoicePlan(
    int Seed,
    TransactionProfileConfig Profile,
    IReadOnlyList<SyntheticInvoiceSpec> Invoices)
{
    public int InvoiceLineCount => Invoices.Sum(invoice => invoice.Lines.Count);
}

public sealed record SyntheticInvoiceSpec(
    string InvoiceNumber,
    long EmployeeId,
    string? CustomerName,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime CompletedAt,
    bool HasManualPriceEdit,
    IReadOnlyList<SyntheticInvoiceLineSpec> Lines)
{
    public decimal SubtotalUsd => Lines.Sum(line => line.LineTotalUsdEffective);

    public decimal TotalUsd => SubtotalUsd;
}

public sealed record SyntheticInvoiceLineSpec(
    long ProductId,
    string ProductBarcode,
    decimal Quantity,
    decimal UnitPriceUsdOriginal,
    decimal LineTotalUsdOriginal,
    decimal LineTotalUsdEffective,
    int SortOrder);
