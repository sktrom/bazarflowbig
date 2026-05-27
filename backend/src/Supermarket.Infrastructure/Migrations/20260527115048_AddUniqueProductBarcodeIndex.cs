using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueProductBarcodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM PRODUCTS
                    GROUP BY Barcode
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000, 'Duplicate product barcodes must be resolved before creating UX_PRODUCTS_Barcode.', 1;
                END
                """);

            migrationBuilder.DropIndex(
                name: "IX_PRODUCTS_Barcode",
                table: "PRODUCTS");

            migrationBuilder.CreateIndex(
                name: "UX_PRODUCTS_Barcode",
                table: "PRODUCTS",
                column: "Barcode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PRODUCTS_Barcode",
                table: "PRODUCTS");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCTS_Barcode",
                table: "PRODUCTS",
                column: "Barcode",
                unique: true);
        }
    }
}
