namespace BazarFlow.PerformanceSeeder;

public static class PerformanceSeederApp
{
    public static Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        var parseResult = SeederCliOptions.Parse(args);
        if (!parseResult.IsSuccess)
        {
            error.WriteLine(parseResult.Error);
            return Task.FromResult(PerformanceSeederExitCodes.ValidationFailed);
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

            return Task.FromResult(PerformanceSeederExitCodes.ValidationFailed);
        }

        var profile = ProfileConfig.Get(options.Profile!);
        var seed = options.Seed ?? SyntheticPreviewGenerator.DefaultSeed;
        var databaseName = ConnectionStringInspector.GetDatabaseName(connectionString!);

        PrintWarning(output, options, databaseName!, seed);

        if (!options.DryRun)
        {
            error.WriteLine("Data writing is not implemented in V2-06B-1. Use --dry-run.");
            return Task.FromResult(PerformanceSeederExitCodes.ImplementationPending);
        }

        PrintDryRun(output, profile, databaseName!, seed, options.Reset);
        return Task.FromResult(PerformanceSeederExitCodes.Success);
    }

    private static void PrintWarning(TextWriter output, SeederCliOptions options, string databaseName, int seed)
    {
        output.WriteLine("============================================================");
        output.WriteLine(" BazarFlow Performance Seeder - V2-06B-1 DRY-RUN SKELETON");
        output.WriteLine(" Synthetic data only. Production databases are forbidden.");
        output.WriteLine(" No database writes or reset operations are implemented here.");
        output.WriteLine("============================================================");
        output.WriteLine($"Profile: {options.Profile}");
        output.WriteLine($"Database: {databaseName}");
        output.WriteLine($"Seed: {seed}");
        output.WriteLine($"Reset requested: {options.Reset}");
        output.WriteLine();
    }

    private static void PrintDryRun(TextWriter output, ProfileConfig profile, string databaseName, int seed, bool reset)
    {
        output.WriteLine("Dry-run summary");
        output.WriteLine($"Profile: {profile.Name}");
        output.WriteLine($"Database name: {databaseName}");
        output.WriteLine($"Seed: {seed}");
        output.WriteLine($"Reset enabled: {reset}");
        if (reset)
        {
            output.WriteLine("Reset would run after safety validation in a future phase. No reset is executed in V2-06B-1.");
        }

        output.WriteLine("Counts:");
        output.WriteLine($"  Categories: {profile.Categories}");
        output.WriteLine($"  Suppliers: {profile.Suppliers}");
        output.WriteLine($"  Products: {profile.Products}");
        output.WriteLine($"  Employees: {profile.Employees}");
        output.WriteLine($"  Devices: {profile.Devices}");
        output.WriteLine($"  Invoices: {profile.Invoices}");
        output.WriteLine("Samples:");

        for (var i = 1; i <= 3; i++)
        {
            output.WriteLine($"  Barcode {i}: {SyntheticPreviewGenerator.Barcode(seed, i)}");
        }

        for (var i = 1; i <= 3; i++)
        {
            output.WriteLine($"  Product {i}: {SyntheticPreviewGenerator.ProductName(seed, i)}");
        }

        for (var i = 1; i <= Math.Min(3, profile.Employees); i++)
        {
            output.WriteLine($"  Employee {i}: {SyntheticPreviewGenerator.EmployeeUsername(seed, i)}");
        }
    }
}
