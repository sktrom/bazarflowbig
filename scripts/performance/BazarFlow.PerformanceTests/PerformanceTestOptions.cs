namespace BazarFlow.PerformanceTests;

public sealed record PerformanceTestOptions(
    Uri BaseUrl,
    string? Username,
    string? Password,
    string? DeviceCode,
    int DurationSeconds,
    int Users,
    string Output,
    bool AllowNonLocalhost)
{
    public const string DefaultBaseUrl = "http://localhost:5070";
    public const int DefaultDurationSeconds = 60;
    public const int DefaultUsers = 1;
    public const string DefaultOutput = "scripts/performance/results";

    public static ParseResult Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                return ParseResult.Fail($"Unexpected argument '{arg}'.");
            }

            var key = arg[2..];
            if (key.Equals("allow-non-localhost", StringComparison.OrdinalIgnoreCase))
            {
                flags.Add(key);
                continue;
            }

            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return ParseResult.Fail($"Missing value for --{key}.");
            }

            values[key] = args[++i];
        }

        var baseUrlText = GetValue(values, "baseUrl", "BASE_URL") ?? DefaultBaseUrl;
        if (!Uri.TryCreate(baseUrlText, UriKind.Absolute, out var baseUrl))
        {
            return ParseResult.Fail("--baseUrl must be an absolute URL.");
        }

        var durationText = GetValue(values, "duration", "DURATION_SECONDS") ?? DefaultDurationSeconds.ToString();
        if (!int.TryParse(durationText, out var durationSeconds) || durationSeconds < 1 || durationSeconds > 600)
        {
            return ParseResult.Fail("--duration must be between 1 and 600 seconds.");
        }

        var usersText = GetValue(values, "users", "CONCURRENT_USERS") ?? DefaultUsers.ToString();
        if (!int.TryParse(usersText, out var users) || users < 1 || users > 10)
        {
            return ParseResult.Fail("--users must be between 1 and 10 for V2-06D smoke tests.");
        }

        var options = new PerformanceTestOptions(
            NormalizeBaseUrl(baseUrl),
            GetValue(values, "username", "USERNAME"),
            GetValue(values, "password", "PASSWORD"),
            GetValue(values, "deviceCode", "DEVICE_CODE"),
            durationSeconds,
            users,
            GetValue(values, "output", "PERF_RESULTS_DIR") ?? DefaultOutput,
            flags.Contains("allow-non-localhost"));

        var safetyError = ValidateSafety(options);
        return safetyError is null ? ParseResult.Success(options) : ParseResult.Fail(safetyError);
    }

    private static string? ValidateSafety(PerformanceTestOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return "Missing username. Use --username or USERNAME.";
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            return "Missing password. Use --password or PASSWORD.";
        }

        if (string.IsNullOrWhiteSpace(options.DeviceCode))
        {
            return "Missing device code. Use --deviceCode or DEVICE_CODE.";
        }

        var host = options.BaseUrl.Host;
        var isLocalhost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                          host.Equals("::1", StringComparison.OrdinalIgnoreCase);

        if (!isLocalhost && !options.AllowNonLocalhost)
        {
            return "Refusing non-localhost baseUrl. Pass --allow-non-localhost only for an approved performance environment.";
        }

        if (LooksProductionLike(options.BaseUrl))
        {
            return "Refusing production-like baseUrl.";
        }

        return null;
    }

    private static bool LooksProductionLike(Uri uri)
    {
        var text = uri.ToString();
        return text.Contains("prod", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("production", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri NormalizeBaseUrl(Uri baseUrl)
    {
        var text = baseUrl.ToString().TrimEnd('/');
        return new Uri(text, UriKind.Absolute);
    }

    private static string? GetValue(IReadOnlyDictionary<string, string?> values, string cliKey, string envKey)
    {
        if (values.TryGetValue(cliKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var env = Environment.GetEnvironmentVariable(envKey);
        return string.IsNullOrWhiteSpace(env) ? null : env;
    }
}

public sealed record ParseResult(bool IsSuccess, PerformanceTestOptions? Options, string? Error)
{
    public static ParseResult Success(PerformanceTestOptions options) => new(true, options, null);

    public static ParseResult Fail(string error) => new(false, null, error);
}
