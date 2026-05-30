using BazarFlow.PerformanceSeeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;
using Xunit;

namespace Supermarket.IntegrationTests.PerformanceSeeder;

public class PerformanceSeederReferenceDataWriterTests
{
    [Fact]
    public async Task SeedAsync_SecondRunDoesNotDuplicateSyntheticData()
    {
        await using var db = CreateInMemoryDbContext();
        var plan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);

        await using var writer = new EfReferenceDataWriter(db);
        await writer.SeedAsync(plan, TextWriter.Null);
        await writer.SeedAsync(plan, TextWriter.Null);

        Assert.Equal(plan.Categories.Count, await db.Categories.CountAsync(category => category.Name.StartsWith("BF-PERF Category 12345-")));
        Assert.Equal(plan.Suppliers.Count, await db.Suppliers.CountAsync(supplier => supplier.Name.StartsWith("BF-PERF Supplier 12345-")));
        Assert.Equal(plan.Products.Count, await db.Products.CountAsync(product => product.Barcode.StartsWith("BF-PERF-12345-")));
        Assert.Equal(plan.Employees.Count, await db.Employees.CountAsync(employee => employee.Username.EndsWith(".12345@example.test")));
        Assert.Equal(plan.Devices.Count, await db.PosDevices.CountAsync(device => device.DeviceCode.StartsWith("BF-PERF-DEV-12345-")));
    }

    [Fact]
    public async Task SeedAsync_DoesNotModifyNonSyntheticData()
    {
        await using var db = CreateInMemoryDbContext();
        db.Categories.Add(new Category { Name = "Real Category", IsActive = true });
        await db.SaveChangesAsync();

        var plan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(plan, TextWriter.Null);

        var nonSynthetic = await db.Categories.SingleAsync(category => category.Name == "Real Category");
        Assert.True(nonSynthetic.IsActive);
        Assert.Equal(1, await db.Categories.CountAsync(category => category.Name == "Real Category"));
    }

    private static SupermarketDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SupermarketDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SupermarketDbContext(options);
    }
}
