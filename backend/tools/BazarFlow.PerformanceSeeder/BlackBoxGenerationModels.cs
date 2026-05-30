namespace BazarFlow.PerformanceSeeder;

public sealed record SyntheticDeviceRef(long Id, string DeviceCode);

public sealed record SyntheticInvoiceRef(long Id, string InvoiceNumber, DateTime CreatedAt, DateTime? CompletedAt);

public sealed record SyntheticPurchaseRef(long Id, string InvoiceNumber, DateTime CreatedAt, DateTime? CompletedAt);

public sealed record SyntheticBlackBoxEventSpec(
    long? EmployeeId,
    string? DeviceCode,
    string? Route,
    string? PageName,
    string ActionType,
    string? ElementKey,
    string? EntityType,
    string? EntityId,
    string Result,
    string Message,
    string MetadataJson,
    bool MetadataTruncated,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAtUtc);

public sealed record BlackBoxEventPlan(
    int Seed,
    TransactionProfileConfig Profile,
    IReadOnlyList<SyntheticBlackBoxEventSpec> Events);
