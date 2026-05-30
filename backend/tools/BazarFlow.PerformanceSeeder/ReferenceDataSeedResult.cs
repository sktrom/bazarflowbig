namespace BazarFlow.PerformanceSeeder;

public sealed record ReferenceDataSeedResult(
    EntitySeedResult Categories,
    EntitySeedResult Suppliers,
    EntitySeedResult Products,
    EntitySeedResult Employees,
    EntitySeedResult Devices);

public sealed record EntitySeedResult(string EntityName, int Planned, int Existing, int Inserted)
{
    public static EntitySeedResult Empty(string entityName, int planned) => new(entityName, planned, 0, 0);
}
