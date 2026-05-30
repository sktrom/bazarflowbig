using System.Data.Common;

namespace BazarFlow.PerformanceSeeder;

public static class ConnectionStringInspector
{
    private static readonly string[] DatabaseKeys = ["Database", "Initial Catalog"];
    private static readonly string[] SecretKeys = ["Password", "Pwd", "User ID", "User Id", "UID", "Uid"];

    public static string? GetDatabaseName(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (var key in DatabaseKeys)
            {
                if (builder.TryGetValue(key, out var value) && value is not null)
                {
                    return value.ToString();
                }
            }
        }
        catch (ArgumentException)
        {
            return GetDatabaseNameFromSegments(connectionString);
        }

        return GetDatabaseNameFromSegments(connectionString);
    }

    public static string SanitizeForDiagnostics(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (var key in SecretKeys)
            {
                if (builder.ContainsKey(key))
                {
                    builder[key] = "<redacted>";
                }
            }

            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            return "<invalid connection string>";
        }
    }

    private static string? GetDatabaseNameFromSegments(string connectionString)
    {
        var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            if (DatabaseKeys.Any(databaseKey => string.Equals(databaseKey, key, StringComparison.OrdinalIgnoreCase)))
            {
                return value;
            }
        }

        return null;
    }
}
