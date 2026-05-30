using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Common.Services;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace BazarFlow.PerformanceSeeder;

public sealed class EfReferenceDataWriter : IReferenceDataWriter
{
    private const int ProductBatchSize = 1_000;
    private readonly SupermarketDbContext _context;
    private readonly PasswordHasher _passwordHasher = new();

    public EfReferenceDataWriter(SupermarketDbContext context)
    {
        _context = context;
    }

    public async Task<ReferenceDataSeedResult> SeedAsync(
        ReferenceDataPlan plan,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        var previousAutoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var categoryResult = await SeedCategoriesAsync(plan, output, cancellationToken);
            var supplierResult = await SeedSuppliersAsync(plan, output, cancellationToken);
            var employeeResult = await SeedEmployeesAsync(plan, output, cancellationToken);
            var deviceResult = await SeedDevicesAsync(plan, output, cancellationToken);
            var productResult = await SeedProductsAsync(plan, output, cancellationToken);

            return new ReferenceDataSeedResult(categoryResult, supplierResult, productResult, employeeResult, deviceResult);
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = previousAutoDetectChanges;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task<EntitySeedResult> SeedCategoriesAsync(
        ReferenceDataPlan plan,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var plannedNames = plan.Categories.Select(category => category.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingNames = await _context.Categories
            .AsNoTracking()
            .Where(category => category.Name.StartsWith($"BF-PERF Category {SyntheticPreviewGenerator.SeedToken(plan.Seed)}-"))
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var existingSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = plan.Categories
            .Where(category => !existingSet.Contains(category.Name))
            .Select(category => new Category { Name = category.Name, IsActive = true })
            .ToList();

        if (missing.Count > 0)
        {
            _context.Categories.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        var result = new EntitySeedResult("Categories", plannedNames.Count, existingSet.Count(name => plannedNames.Contains(name)), missing.Count);
        PrintProgress(output, result);
        return result;
    }

    private async Task<EntitySeedResult> SeedSuppliersAsync(
        ReferenceDataPlan plan,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var plannedNames = plan.Suppliers.Select(supplier => supplier.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingNames = await _context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.Name.StartsWith($"BF-PERF Supplier {SyntheticPreviewGenerator.SeedToken(plan.Seed)}-"))
            .Select(supplier => supplier.Name)
            .ToListAsync(cancellationToken);

        var existingSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = plan.Suppliers
            .Where(supplier => !existingSet.Contains(supplier.Name))
            .Select(supplier => new Supplier
            {
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                Notes = supplier.Notes,
                IsActive = true,
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt
            })
            .ToList();

        if (missing.Count > 0)
        {
            _context.Suppliers.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        var result = new EntitySeedResult("Suppliers", plannedNames.Count, existingSet.Count(name => plannedNames.Contains(name)), missing.Count);
        PrintProgress(output, result);
        return result;
    }

    private async Task<EntitySeedResult> SeedEmployeesAsync(
        ReferenceDataPlan plan,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var plannedUsernames = plan.Employees.Select(employee => employee.Username).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seedSuffix = $".{SyntheticPreviewGenerator.SeedToken(plan.Seed)}@example.test";
        var existingUsernames = await _context.Employees
            .AsNoTracking()
            .Where(employee => employee.Username.EndsWith(seedSuffix))
            .Select(employee => employee.Username)
            .ToListAsync(cancellationToken);

        var existingSet = existingUsernames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var passwordHash = _passwordHasher.Hash(SyntheticPreviewGenerator.TestOnlyPassword);
        var missing = plan.Employees
            .Where(employee => !existingSet.Contains(employee.Username))
            .Select(employee => new Employee
            {
                FullName = employee.FullName,
                Username = employee.Username,
                Phone = employee.Phone,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt
            })
            .ToList();

        if (missing.Count > 0)
        {
            _context.Employees.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        var result = new EntitySeedResult("Employees", plannedUsernames.Count, existingSet.Count(name => plannedUsernames.Contains(name)), missing.Count);
        PrintProgress(output, result);
        return result;
    }

    private async Task<EntitySeedResult> SeedDevicesAsync(
        ReferenceDataPlan plan,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var plannedCodes = plan.Devices.Select(device => device.DeviceCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCodes = await _context.PosDevices
            .AsNoTracking()
            .Where(device => device.DeviceCode.StartsWith($"BF-PERF-DEV-{SyntheticPreviewGenerator.SeedToken(plan.Seed)}-"))
            .Select(device => device.DeviceCode)
            .ToListAsync(cancellationToken);

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = plan.Devices
            .Where(device => !existingSet.Contains(device.DeviceCode))
            .Select(device => new PosDevice
            {
                DeviceCode = device.DeviceCode,
                DeviceName = device.DeviceName,
                Notes = device.Notes,
                IsActive = true,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            })
            .ToList();

        if (missing.Count > 0)
        {
            _context.PosDevices.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        var result = new EntitySeedResult("Devices", plannedCodes.Count, existingSet.Count(code => plannedCodes.Contains(code)), missing.Count);
        PrintProgress(output, result);
        return result;
    }

    private async Task<EntitySeedResult> SeedProductsAsync(
        ReferenceDataPlan plan,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var plannedBarcodes = plan.Products.Select(product => product.Barcode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingBarcodes = await _context.Products
            .AsNoTracking()
            .Where(product => product.Barcode.StartsWith($"BF-PERF-{SyntheticPreviewGenerator.SeedToken(plan.Seed)}-"))
            .Select(product => product.Barcode)
            .ToListAsync(cancellationToken);

        var existingSet = existingBarcodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categoryIds = await _context.Categories
            .AsNoTracking()
            .Where(category => category.Name.StartsWith($"BF-PERF Category {SyntheticPreviewGenerator.SeedToken(plan.Seed)}-"))
            .OrderBy(category => category.Name)
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);

        if (categoryIds.Count == 0)
        {
            throw new InvalidOperationException("Synthetic categories are required before products can be seeded.");
        }

        var inserted = 0;
        foreach (var batch in plan.Products.Where(product => !existingSet.Contains(product.Barcode)).Chunk(ProductBatchSize))
        {
            var entities = batch.Select(product => new Product
            {
                Name = product.Name,
                Barcode = product.Barcode,
                CategoryId = categoryIds[product.CategoryIndex % categoryIds.Count],
                BaseUnit = product.BaseUnit,
                PriceUsd = product.PriceUsd,
                HasCarton = product.HasCarton,
                CartonQuantity = product.CartonQuantity,
                CartonPriceUsd = product.CartonPriceUsd,
                HasExpiry = product.HasExpiry,
                IsActive = true,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            }).ToList();

            if (entities.Count == 0)
            {
                continue;
            }

            _context.Products.AddRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            inserted += entities.Count;
            output.WriteLine($"Products batch inserted: {inserted}/{plannedBarcodes.Count}");
        }

        var result = new EntitySeedResult("Products", plannedBarcodes.Count, existingSet.Count(barcode => plannedBarcodes.Contains(barcode)), inserted);
        PrintProgress(output, result);
        return result;
    }

    private static void PrintProgress(TextWriter output, EntitySeedResult result)
    {
        output.WriteLine($"{result.EntityName}: planned={result.Planned}, existing={result.Existing}, inserted={result.Inserted}");
    }
}
