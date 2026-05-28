using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace BazarFlow.DbMigrator;

public static class ConnectionStringSanitizer
{
    public static string Sanitize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "<empty>";
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (ContainsConnectionStringKey(connectionString, "Password") || ContainsConnectionStringKey(connectionString, "Pwd"))
            {
                builder.Password = "<redacted>";
            }
            else
            {
                builder.Remove("Password");
            }

            if (ContainsConnectionStringKey(connectionString, "User ID") || ContainsConnectionStringKey(connectionString, "Uid"))
            {
                builder.UserID = "<redacted>";
            }
            else
            {
                builder.Remove("User ID");
            }

            return builder.ConnectionString;
        }
        catch
        {
            var sanitized = Regex.Replace(connectionString, @"(?i)(password|pwd)\s*=\s*[^;]*", "$1=<redacted>");
            sanitized = Regex.Replace(sanitized, @"(?i)(user\s+id|uid)\s*=\s*[^;]*", "$1=<redacted>");
            return sanitized;
        }
    }

    public static string SanitizeException(Exception exception)
    {
        return SanitizeFreeText(exception.Message);
    }

    public static string SanitizeFreeText(string value)
    {
        var sanitized = Regex.Replace(value, @"(?i)(password|pwd)\s*=\s*[^;,\r\n]*", "$1=<redacted>");
        sanitized = Regex.Replace(sanitized, @"(?i)(user\s+id|uid)\s*=\s*[^;,\r\n]*", "$1=<redacted>");
        return sanitized;
    }

    private static bool ContainsConnectionStringKey(string connectionString, string key)
    {
        return Regex.IsMatch(connectionString, $@"(^|;)\s*{Regex.Escape(key)}\s*=", RegexOptions.IgnoreCase);
    }
}
