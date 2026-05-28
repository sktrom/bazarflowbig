using Microsoft.Data.SqlClient;

namespace BazarFlow.DbMigrator;

public sealed class DatabaseReadinessChecks
{
    private static readonly string[] RequiredTables =
    [
        "EMPLOYEES",
        "APP_SCREENS",
        "PRODUCTS",
        "CASH_SESSIONS",
        "SUPPLIERS",
        "PURCHASE_INVOICES"
    ];

    public async Task<IReadOnlyList<string>> GetMissingRequiredTablesAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        var missingTables = new List<string>();
        foreach (var tableName in RequiredTables)
        {
            if (!await TableExistsAsync(connection, tableName, cancellationToken))
            {
                missingTables.Add(tableName);
            }
        }

        return missingTables;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = @tableName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }
}
