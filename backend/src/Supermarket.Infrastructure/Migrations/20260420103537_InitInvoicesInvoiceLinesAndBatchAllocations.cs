using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitInvoicesInvoiceLinesAndBatchAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INVOICES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SuspensionReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InvoiceDiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InvoiceDiscountValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SubtotalUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExchangeRateSypSnapshot = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalSyp = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    HasManualPriceEdit = table.Column<bool>(type: "bit", nullable: false),
                    HasAdjustmentRequest = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INVOICES_EMPLOYEES_OriginalEmployeeId",
                        column: x => x.OriginalEmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "INVOICE_LINES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPriceUsdOriginal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotalUsdOriginal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotalUsdEffective = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsPriceOverridden = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICE_LINES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INVOICE_LINES_INVOICES_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "INVOICES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INVOICE_LINES_PRODUCTS_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "INVOICE_LINE_BATCH_ALLOCATIONS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceLineId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AllocationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICE_LINE_BATCH_ALLOCATIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INVOICE_LINE_BATCH_ALLOCATIONS_INVOICE_LINES_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalTable: "INVOICE_LINES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INVOICE_LINE_BATCH_ALLOCATIONS_PRODUCT_BATCHES_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PRODUCT_BATCHES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_LINE_BATCH_ALLOCATIONS_BatchId",
                table: "INVOICE_LINE_BATCH_ALLOCATIONS",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_LINE_BATCH_ALLOCATIONS_InvoiceLineId_BatchId",
                table: "INVOICE_LINE_BATCH_ALLOCATIONS",
                columns: new[] { "InvoiceLineId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_LINES_InvoiceId",
                table: "INVOICE_LINES",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_LINES_ProductId",
                table: "INVOICE_LINES",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICES_InvoiceNumber",
                table: "INVOICES",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVOICES_OriginalEmployeeId",
                table: "INVOICES",
                column: "OriginalEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INVOICE_LINE_BATCH_ALLOCATIONS");

            migrationBuilder.DropTable(
                name: "INVOICE_LINES");

            migrationBuilder.DropTable(
                name: "INVOICES");
        }
    }
}
