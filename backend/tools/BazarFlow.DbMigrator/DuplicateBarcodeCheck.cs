using Microsoft.Data.SqlClient;

namespace BazarFlow.DbMigrator;

public sealed record DuplicateBarcode(string Barcode, int Count);

public sealed class DuplicateBarcodeCheck
{
    public async Task<IReadOnlyList<DuplicateBarcode>> FindDuplicatesAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        if (!await ProductsTableExistsAsync(connection, cancellationToken))
        {
            return Array.Empty<DuplicateBarcode>();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (20) Barcode, COUNT(*) AS DuplicateCount
            FROM PRODUCTS
            GROUP BY Barcode
            HAVING COUNT(*) > 1
            ORDER BY DuplicateCount DESC, Barcode ASC;
            """;

        var duplicates = new List<DuplicateBarcode>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            duplicates.Add(new DuplicateBarcode(reader.GetString(0), reader.GetInt32(1)));
        }

        return duplicates;
    }

    public static bool IsDuplicateBarcodeFailure(Exception exception)
    {
        var message = exception.ToString();
        return message.Contains("Duplicate product barcodes", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UX_PRODUCTS_Barcode", StringComparison.OrdinalIgnoreCase)
            || message.Contains("IX_PRODUCTS_Barcode", StringComparison.OrdinalIgnoreCase)
            || (message.Contains("PRODUCTS", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Barcode", StringComparison.OrdinalIgnoreCase)
                && (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("unique", StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task<bool> ProductsTableExistsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = 'PRODUCTS';
            """;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }
}
