using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoicesAndLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PURCHASE_INVOICES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    ExternalInvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubtotalUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    TotalUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PURCHASE_INVOICES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PURCHASE_INVOICES_EMPLOYEES_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PURCHASE_INVOICES_SUPPLIERS_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "SUPPLIERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PURCHASE_INVOICE_LINES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCostUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotalUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PURCHASE_INVOICE_LINES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PURCHASE_INVOICE_LINES_PRODUCTS_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PURCHASE_INVOICE_LINES_PURCHASE_INVOICES_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PURCHASE_INVOICES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICE_LINES_ProductId",
                table: "PURCHASE_INVOICE_LINES",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICE_LINES_PurchaseInvoiceId",
                table: "PURCHASE_INVOICE_LINES",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICE_LINES_PurchaseInvoiceId_ProductId",
                table: "PURCHASE_INVOICE_LINES",
                columns: new[] { "PurchaseInvoiceId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICES_CreatedByEmployeeId",
                table: "PURCHASE_INVOICES",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICES_InvoiceNumber",
                table: "PURCHASE_INVOICES",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICES_Status_CreatedAt",
                table: "PURCHASE_INVOICES",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICES_SupplierId",
                table: "PURCHASE_INVOICES",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PURCHASE_INVOICE_LINES");

            migrationBuilder.DropTable(
                name: "PURCHASE_INVOICES");
        }
    }
}
