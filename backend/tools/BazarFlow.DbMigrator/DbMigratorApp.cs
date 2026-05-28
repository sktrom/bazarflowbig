using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Supermarket.Infrastructure.Persistence;

namespace BazarFlow.DbMigrator;

public static class DbMigratorApp
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = new ConnectionStringResolver().Resolve(args);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                error.WriteLine("DefaultConnection is not configured. Pass --connection or set ConnectionStrings__DefaultConnection or BAZARFLOW_CONNECTION_STRING.");
                return MigrationExitCodes.MissingConnectionString;
            }

            output.WriteLine("BazarFlow DbMigrator starting...");
            output.WriteLine($"Target database: {ConnectionStringSanitizer.Sanitize(connectionString)}");

            var targetDatabaseName = GetTargetDatabaseName(connectionString);
            var serverConnectionString = CreateServerConnectionString(connectionString);
            await using var serverConnection = new SqlConnection(serverConnectionString);
            try
            {
                await serverConnection.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                error.WriteLine($"SQL connection failed: {ConnectionStringSanitizer.SanitizeException(ex)}");
                return MigrationExitCodes.SqlConnectionFailed;
            }

            var duplicateBarcodeCheck = new DuplicateBarcodeCheck();
            if (!string.IsNullOrWhiteSpace(targetDatabaseName) && await DatabaseExistsAsync(serverConnection, targetDatabaseName, cancellationToken))
            {
                await using var duplicateCheckConnection = new SqlConnection(connectionString);
                await duplicateCheckConnection.OpenAsync(cancellationToken);
                var duplicates = await duplicateBarcodeCheck.FindDuplicatesAsync(duplicateCheckConnection, cancellationToken);
                if (duplicates.Count > 0)
                {
                    error.WriteLine("Duplicate product barcodes block migration. Resolve these rows before running migrations.");
                    foreach (var duplicate in duplicates.Take(20))
                    {
                        error.WriteLine($"Barcode '{duplicate.Barcode}' appears {duplicate.Count} times.");
                    }

                    return MigrationExitCodes.DuplicateBarcodeBlocksMigration;
                }
            }

            var options = new DbContextOptionsBuilder<SupermarketDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using (var context = new SupermarketDbContext(options))
            {
                try
                {
                    await context.Database.MigrateAsync(cancellationToken);
                }
                catch (Exception ex) when (DuplicateBarcodeCheck.IsDuplicateBarcodeFailure(ex))
                {
                    error.WriteLine($"Migration blocked by duplicate product barcodes: {ConnectionStringSanitizer.SanitizeException(ex)}");
                    return MigrationExitCodes.DuplicateBarcodeBlocksMigration;
                }
                catch (Exception ex)
                {
                    error.WriteLine($"Migration failed: {ConnectionStringSanitizer.SanitizeException(ex)}");
                    return MigrationExitCodes.MigrationFailed;
                }
            }

            var readinessChecks = new DatabaseReadinessChecks();
            await using var readinessConnection = new SqlConnection(connectionString);
            await readinessConnection.OpenAsync(cancellationToken);
            var missingTables = await readinessChecks.GetMissingRequiredTablesAsync(readinessConnection, cancellationToken);
            if (missingTables.Count > 0)
            {
                error.WriteLine($"Database readiness check failed. Missing tables: {string.Join(", ", missingTables)}");
                return MigrationExitCodes.MigrationFailed;
            }

            output.WriteLine("BazarFlow database is ready.");
            return MigrationExitCodes.Success;
        }
        catch (Exception ex)
        {
            error.WriteLine($"Unexpected error: {ConnectionStringSanitizer.SanitizeException(ex)}");
            return MigrationExitCodes.UnexpectedError;
        }
    }

    private static string? GetTargetDatabaseName(string connectionString)
    {
        try
        {
            return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }
        catch
        {
            return null;
        }
    }

    private static string CreateServerConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            builder.InitialCatalog = "master";
        }

        return builder.ConnectionString;
    }

    private static async Task<bool> DatabaseExistsAsync(SqlConnection connection, string databaseName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN DB_ID(@databaseName) IS NULL THEN 0 ELSE 1 END;";
        command.Parameters.AddWithValue("@databaseName", databaseName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }
}
