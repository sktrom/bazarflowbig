namespace BazarFlow.PerformanceSeeder;

public sealed record SeederCliOptions(
    string? Profile,
    string? ConnectionString,
    int? Seed,
    bool Confirm,
    bool Reset,
    bool ConfirmReset,
    bool DryRun,
    bool IncludeTransactions)
{
    public static ParseResult<SeederCliOptions> Parse(IReadOnlyList<string> args)
    {
        string? profile = null;
        string? connectionString = null;
        int? seed = null;
        var confirm = false;
        var reset = false;
        var confirmReset = false;
        var dryRun = false;
        var includeTransactions = false;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--profile":
                    if (!TryReadValue(args, ref i, out profile))
                    {
                        return ParseResult<SeederCliOptions>.Fail("--profile requires a value.");
                    }

                    profile = profile?.Trim().ToLowerInvariant();
                    break;
                case "--connection":
                    if (!TryReadValue(args, ref i, out connectionString))
                    {
                        return ParseResult<SeederCliOptions>.Fail("--connection requires a value.");
                    }

                    break;
                case "--seed":
                    if (!TryReadValue(args, ref i, out var seedValue))
                    {
                        return ParseResult<SeederCliOptions>.Fail("--seed requires a value.");
                    }

                    if (!int.TryParse(seedValue, out var parsedSeed))
                    {
                        return ParseResult<SeederCliOptions>.Fail("--seed must be an integer.");
                    }

                    seed = parsedSeed;
                    break;
                case "--confirm":
                    confirm = true;
                    break;
                case "--reset":
                    reset = true;
                    break;
                case "--confirm-reset":
                    confirmReset = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--include-transactions":
                    includeTransactions = true;
                    break;
                default:
                    return ParseResult<SeederCliOptions>.Fail($"Unknown option '{arg}'.");
            }
        }

        return ParseResult<SeederCliOptions>.Ok(
            new SeederCliOptions(profile, connectionString, seed, confirm, reset, confirmReset, dryRun, includeTransactions));
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string? value)
    {
        value = null;
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        value = args[++index];
        return true;
    }
}
