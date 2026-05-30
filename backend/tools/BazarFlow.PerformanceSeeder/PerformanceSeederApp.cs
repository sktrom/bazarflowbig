namespace BazarFlow.PerformanceSeeder;

public static class PerformanceSeederApp
{
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<string, IReferenceDataWriter>? writerFactory = null,
        CancellationToken cancellationToken = default)
    {
        writerFactory ??= DefaultReferenceDataWriterFactory.Create;

        var parseResult = SeederCliOptions.Parse(args);
        if (!parseResult.IsSuccess)
        {
            error.WriteLine(parseResult.Error);
            return PerformanceSeederExitCodes.ValidationFailed;
        }

        var options = parseResult.Value!;
        var connectionString = options.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("BAZARFLOW_PERF_SEED_CONNECTION");
        }

        var validation = SafetyValidator.Validate(options, connectionString);
        if (!validation.IsSuccess)
        {
            foreach (var validationError in validation.Errors)
            {
                error.WriteLine(validationError);
            }

            return PerformanceSeederExitCodes.ValidationFailed;
        }

        var profile = ProfileConfig.Get(options.Profile!);
        var seed = options.Seed ?? SyntheticPreviewGenerator.DefaultSeed;
        var databaseName = ConnectionStringInspector.GetDatabaseName(connectionString!);
        var plan = ReferenceDataGenerator.Generate(profile, seed);

        PrintWarning(output, options, databaseName!, seed);

        if (options.DryRun)
        {
            PrintDryRun(output, plan, databaseName!, options.Reset);
            return PerformanceSeederExitCodes.Success;
        }

        if (options.Reset)
        {
            error.WriteLine("Reset is not implemented in V2-06B-2. Remove --reset.");
            return PerformanceSeederExitCodes.ImplementationPending;
        }

        try
        {
            output.WriteLine("Starting core reference data generation.");
            await using var writer = writerFactory(connectionString!);
            var result = await writer.SeedAsync(plan, output, cancellationToken);
            PrintWriteSummary(output, result);
            return PerformanceSeederExitCodes.Success;
        }
        catch (Exception ex)
        {
            error.WriteLine($"Reference data generation failed: {ex.GetType().Name}: {ex.Message}");
            return PerformanceSeederExitCodes.SeedFailed;
        }
    }

    private static void PrintWarning(TextWriter output, SeederCliOptions options, string databaseName, int seed)
    {
        output.WriteLine("============================================================");
        output.WriteLine(" BazarFlow Performance Seeder - V2-06B-2 CORE REFERENCE DATA");
        output.WriteLine(" Synthetic data only. Production databases are forbidden.");
        output.WriteLine(" Writes are limited to categories, suppliers, products, employees, and devices.");
        output.WriteLine(" Reset operations are not implemented.");
        output.WriteLine("============================================================");
        output.WriteLine($"Profile: {options.Profile}");
        output.WriteLine($"Database: {databaseName}");
        output.WriteLine($"Seed: {seed}");
        output.WriteLine($"Reset requested: {options.Reset}");
        output.WriteLine();
    }

    private static void PrintDryRun(TextWriter output, ReferenceDataPlan plan, string databaseName, bool reset)
    {
        output.WriteLine("Dry-run summary");
        output.WriteLine($"Profile: {plan.Profile.Name}");
        output.WriteLine($"Database name: {databaseName}");
        output.WriteLine($"Seed: {plan.Seed}");
        output.WriteLine($"Reset enabled: {reset}");
        if (reset)
        {
            output.WriteLine("Reset is deferred. No reset is executed in V2-06B-2.");
        }

        output.WriteLine("Counts:");
        output.WriteLine($"  Categories: {plan.Categories.Count}");
        output.WriteLine($"  Suppliers: {plan.Suppliers.Count}");
        output.WriteLine($"  Products: {plan.Products.Count}");
        output.WriteLine($"  Employees: {plan.Employees.Count}");
        output.WriteLine($"  Devices: {plan.Devices.Count}");
        output.WriteLine("Samples:");

        foreach (var category in plan.Categories.Take(3))
        {
            output.WriteLine($"  Category: {category.Name}");
        }

        foreach (var supplier in plan.Suppliers.Take(3))
        {
            output.WriteLine($"  Supplier: {supplier.Name}");
        }

        foreach (var product in plan.Products.Take(3))
        {
            output.WriteLine($"  Product: {product.Name}, Barcode: {product.Barcode}, PriceUsd: {product.PriceUsd}");
        }

        foreach (var employee in plan.Employees.Take(3))
        {
            output.WriteLine($"  Employee: {employee.Username}");
        }

        foreach (var device in plan.Devices.Take(3))
        {
            output.WriteLine($"  Device: {device.DeviceCode}");
        }
    }

    private static void PrintWriteSummary(TextWriter output, ReferenceDataSeedResult result)
    {
        output.WriteLine("Core reference data generation complete.");
        output.WriteLine($"Categories: planned={result.Categories.Planned}, existing={result.Categories.Existing}, inserted={result.Categories.Inserted}");
        output.WriteLine($"Suppliers: planned={result.Suppliers.Planned}, existing={result.Suppliers.Existing}, inserted={result.Suppliers.Inserted}");
        output.WriteLine($"Products: planned={result.Products.Planned}, existing={result.Products.Existing}, inserted={result.Products.Inserted}");
        output.WriteLine($"Employees: planned={result.Employees.Planned}, existing={result.Employees.Existing}, inserted={result.Employees.Inserted}");
        output.WriteLine($"Devices: planned={result.Devices.Planned}, existing={result.Devices.Existing}, inserted={result.Devices.Inserted}");
    }
}
