using BazarFlow.PerformanceSeeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
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

    [Fact]
    public async Task SeedPurchasesAsync_RefusesWhenProductsMissing()
    {
        await using var db = CreateInMemoryDbContext();
        await using var writer = new EfReferenceDataWriter(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.SeedPurchasesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null));

        Assert.Equal("Run core reference data generation first.", exception.Message);
    }

    [Fact]
    public async Task SeedPurchasesAsync_SecondRunDoesNotDuplicatePurchasesOrBatches()
    {
        await using var db = CreateInMemoryDbContext();
        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);
        await writer.SeedPurchasesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedPurchasesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);

        var transactionProfile = TransactionProfileConfig.Get("small");
        Assert.Equal(transactionProfile.Purchases, await db.PurchaseInvoices.CountAsync(invoice => invoice.InvoiceNumber.StartsWith("BF-PERF-PUR-12345-")));

        var lineCount = await db.PurchaseInvoiceLines
            .CountAsync(line => line.PurchaseInvoice != null && line.PurchaseInvoice.InvoiceNumber.StartsWith("BF-PERF-PUR-12345-"));
        Assert.InRange(lineCount, transactionProfile.MinimumPurchaseLines, transactionProfile.MaximumPurchaseLines);
        Assert.Equal(lineCount, await db.ProductBatches.CountAsync(batch => batch.EntryInvoiceNumber != null && batch.EntryInvoiceNumber.StartsWith("BF-PERF-EXT-PUR-12345-")));
    }

    [Fact]
    public async Task SeedPurchasesAsync_DoesNotModifyNonSyntheticPurchases()
    {
        await using var db = CreateInMemoryDbContext();
        db.PurchaseInvoices.Add(new PurchaseInvoice
        {
            InvoiceNumber = "REAL-PUR-1",
            SupplierId = 999,
            CreatedByEmployeeId = 999,
            CompletedByEmployeeId = 999,
            Status = PurchaseInvoiceStatus.Completed,
            SubtotalUsd = 10m,
            TotalUsd = 10m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);
        await writer.SeedPurchasesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);

        var realPurchase = await db.PurchaseInvoices.SingleAsync(invoice => invoice.InvoiceNumber == "REAL-PUR-1");
        Assert.Equal(10m, realPurchase.TotalUsd);
        Assert.Equal(1, await db.PurchaseInvoices.CountAsync(invoice => invoice.InvoiceNumber == "REAL-PUR-1"));
    }

    [Fact]
    public async Task SeedInvoicesAsync_RefusesWhenProductsMissing()
    {
        await using var db = CreateInMemoryDbContext();
        await using var writer = new EfReferenceDataWriter(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.SeedInvoicesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null));

        Assert.Equal("Run core reference data generation first.", exception.Message);
    }

    [Fact]
    public async Task SeedInvoicesAsync_SecondRunDoesNotDuplicateInvoicesOrLines()
    {
        await using var db = CreateInMemoryDbContext();
        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);
        await writer.SeedInvoicesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedInvoicesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);

        var transactionProfile = TransactionProfileConfig.Get("small");
        var invoiceIds = await db.Invoices
            .Where(invoice => invoice.InvoiceNumber.StartsWith("BF-PERF-INV-12345-"))
            .Select(invoice => invoice.Id)
            .ToListAsync();
        Assert.Equal(transactionProfile.Invoices, invoiceIds.Count);

        var lineCount = await db.InvoiceLines.CountAsync(line => invoiceIds.Contains(line.InvoiceId));
        Assert.InRange(lineCount, transactionProfile.MinimumInvoiceLines, transactionProfile.MaximumInvoiceLines);
    }

    [Fact]
    public async Task SeedInvoicesAsync_DoesNotModifyNonSyntheticInvoices()
    {
        await using var db = CreateInMemoryDbContext();
        db.Employees.Add(new Employee
        {
            Id = 999,
            FullName = "Real Employee",
            Username = "real.employee",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "REAL-INV-1",
            OriginalEmployeeId = 999,
            Status = InvoiceStatus.Completed,
            SubtotalUsd = 10m,
            TotalUsd = 10m,
            HasManualPriceEdit = false,
            HasAdjustmentRequest = false,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);
        await writer.SeedInvoicesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);

        var realInvoice = await db.Invoices.SingleAsync(invoice => invoice.InvoiceNumber == "REAL-INV-1");
        Assert.Equal(10m, realInvoice.TotalUsd);
        Assert.Equal(1, await db.Invoices.CountAsync(invoice => invoice.InvoiceNumber == "REAL-INV-1"));
    }

    [Fact]
    public async Task SeedBlackBoxEventsAsync_RefusesWhenCoreReferencesMissing()
    {
        await using var db = CreateInMemoryDbContext();
        await using var writer = new EfReferenceDataWriter(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.SeedBlackBoxEventsAsync(12345, ProfileConfig.Get("small"), TextWriter.Null));

        Assert.Equal("Run core reference data generation first.", exception.Message);
    }

    [Fact]
    public async Task SeedBlackBoxEventsAsync_RefusesWhenTransactionsMissing()
    {
        await using var db = CreateInMemoryDbContext();
        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.SeedBlackBoxEventsAsync(12345, ProfileConfig.Get("small"), TextWriter.Null));

        Assert.Equal("Run transactional generation first.", exception.Message);
    }

    [Fact]
    public async Task SeedBlackBoxEventsAsync_SecondRunDoesNotDuplicateEvents()
    {
        await using var db = CreateInMemoryDbContext();
        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        var transactionProfile = TransactionProfileConfig.Get("small");
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);
        await writer.SeedPurchasesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedInvoicesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedBlackBoxEventsAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedBlackBoxEventsAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);

        Assert.Equal(
            transactionProfile.BlackBoxEvents,
            await db.BlackBoxEvents.CountAsync(evt => evt.Message != null && evt.Message.StartsWith("Synthetic event BF-PERF-BBX-12345-")));
    }

    [Fact]
    public async Task SeedBlackBoxEventsAsync_DoesNotModifyNonSyntheticEvents()
    {
        await using var db = CreateInMemoryDbContext();
        db.BlackBoxEvents.Add(new BlackBoxEvent
        {
            ActionType = "REAL_ACTION",
            Result = "Success",
            Message = "Real event",
            MetadataJson = "{\"synthetic\":false}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var referencePlan = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        await using var writer = new EfReferenceDataWriter(db);

        await writer.SeedAsync(referencePlan, TextWriter.Null);
        await writer.SeedPurchasesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedInvoicesAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);
        await writer.SeedBlackBoxEventsAsync(12345, ProfileConfig.Get("small"), TextWriter.Null);

        var realEvent = await db.BlackBoxEvents.SingleAsync(evt => evt.Message == "Real event");
        Assert.Equal("REAL_ACTION", realEvent.ActionType);
        Assert.Equal(1, await db.BlackBoxEvents.CountAsync(evt => evt.Message == "Real event"));
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
