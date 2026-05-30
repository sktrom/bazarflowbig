namespace BazarFlow.PerformanceSeeder;

public sealed record ReferenceDataPlan(
    int Seed,
    ProfileConfig Profile,
    IReadOnlyList<SyntheticCategorySpec> Categories,
    IReadOnlyList<SyntheticSupplierSpec> Suppliers,
    IReadOnlyList<SyntheticProductSpec> Products,
    IReadOnlyList<SyntheticEmployeeSpec> Employees,
    IReadOnlyList<SyntheticDeviceSpec> Devices);

public sealed record SyntheticCategorySpec(string Name);

public sealed record SyntheticSupplierSpec(
    string Name,
    string Email,
    string Phone,
    string Address,
    string Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SyntheticProductSpec(
    string Name,
    string Barcode,
    string BaseUnit,
    decimal PriceUsd,
    bool HasCarton,
    int? CartonQuantity,
    decimal? CartonPriceUsd,
    bool HasExpiry,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CategoryIndex);

public sealed record SyntheticEmployeeSpec(
    string FullName,
    string Username,
    string Phone,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SyntheticDeviceSpec(
    string DeviceCode,
    string DeviceName,
    string Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
