namespace BazarFlow.PerformanceSeeder;

public static class SafetyValidator
{
    private static readonly string[] RequiredSafeNameMarkers = ["Performance", "Perf", "Test", "Load"];
    private static readonly string[] ProductionLikeNames = ["BazarFlow", "BazarFlowProd", "Production", "Prod"];

    public static SafetyValidationResult Validate(SeederCliOptions options, string? connectionString)
    {
        var errors = new List<string>();

        if (!options.Confirm)
        {
            errors.Add("Refusing to run without --confirm.");
        }

        if (string.IsNullOrWhiteSpace(options.Profile))
        {
            errors.Add("Refusing to run without --profile small|medium|large.");
        }
        else if (!ProfileConfig.IsSupported(options.Profile))
        {
            errors.Add($"Unsupported profile '{options.Profile}'. Use small, medium, or large.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("Connection string is required via --connection or BAZARFLOW_PERF_SEED_CONNECTION.");
        }

        var databaseName = string.IsNullOrWhiteSpace(connectionString)
            ? null
            : ConnectionStringInspector.GetDatabaseName(connectionString);

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            errors.Add("Connection string must include a database name.");
        }
        else if (!IsSafeDatabaseName(databaseName))
        {
            errors.Add($"Refusing database '{databaseName}'. Database name must contain Performance, Perf, Test, or Load and must not be production-like.");
        }

        if (options.Reset && !options.ConfirmReset)
        {
            errors.Add("Refusing reset without --confirm-reset.");
        }

        return errors.Count == 0
            ? SafetyValidationResult.Success()
            : SafetyValidationResult.Fail(errors);
    }

    public static bool IsSafeDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return false;
        }

        var hasSafeMarker = RequiredSafeNameMarkers.Any(marker =>
            databaseName.Contains(marker, StringComparison.OrdinalIgnoreCase));
        if (!hasSafeMarker)
        {
            return false;
        }

        return !IsProductionLikeDatabaseName(databaseName);
    }

    public static bool IsProductionLikeDatabaseName(string databaseName)
    {
        var normalized = databaseName.Trim();

        foreach (var productionName in ProductionLikeNames)
        {
            if (string.Equals(normalized, productionName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return normalized.Contains("Production", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("Prod", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("_Prod", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("-Prod", StringComparison.OrdinalIgnoreCase);
    }
}
