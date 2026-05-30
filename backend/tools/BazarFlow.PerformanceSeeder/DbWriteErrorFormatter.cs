using System.Text.RegularExpressions;

namespace BazarFlow.PerformanceSeeder;

public static class DbWriteErrorFormatter
{
    private static readonly Regex SensitiveKeyValuePattern = new(
        @"(?i)\b(password|pwd|user\s*id|uid)\s*=\s*[^;\s]+",
        RegexOptions.Compiled);

    public static void Write(TextWriter error, Exception exception)
    {
        error.WriteLine($"Reference data generation failed: {Classify(exception)}");
        error.WriteLine($"Exception: {exception.GetType().Name}: {Sanitize(exception.Message)}");

        if (exception.InnerException is not null)
        {
            error.WriteLine($"Inner exception: {exception.InnerException.GetType().Name}: {Sanitize(exception.InnerException.Message)}");
        }

        error.WriteLine("Run BazarFlow.DbMigrator against the performance database before seeding.");
    }

    public static string Classify(Exception exception)
    {
        var text = FlattenMessages(exception);

        if (ContainsAny(text, "login failed", "permission", "access denied", "not authorized", "not permitted"))
        {
            return "PERMISSION_DENIED";
        }

        if (ContainsAny(text, "invalid object name", "cannot open database", "database does not exist", "could not find stored procedure"))
        {
            return "DATABASE_NOT_FOUND_OR_NOT_MIGRATED";
        }

        if (ContainsAny(
            text,
            "transient failure",
            "network-related",
            "server was not found",
            "error occurred while establishing",
            "timeout expired",
            "connection timeout",
            "provider: tcp provider",
            "provider: named pipes provider"))
        {
            return "SQL_CONNECTION_FAILED";
        }

        return "DB_WRITE_FAILED";
    }

    public static string Sanitize(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return SensitiveKeyValuePattern.Replace(message, match =>
        {
            var separatorIndex = match.Value.IndexOf('=');
            return separatorIndex < 0 ? "<redacted>" : $"{match.Value[..separatorIndex]}=<redacted>";
        });
    }

    private static string FlattenMessages(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" ", messages);
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
