using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompletePurchaseCreatesBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "PURCHASE_INVOICES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompletedByEmployeeId",
                table: "PURCHASE_INVOICES",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PurchaseInvoiceLineId",
                table: "PRODUCT_BATCHES",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostUsd",
                table: "PRODUCT_BATCHES",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PURCHASE_INVOICES_CompletedByEmployeeId",
                table: "PURCHASE_INVOICES",
                column: "CompletedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_BATCHES_PurchaseInvoiceLineId",
                table: "PRODUCT_BATCHES",
                column: "PurchaseInvoiceLineId",
                unique: true,
                filter: "[PurchaseInvoiceLineId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PRODUCT_BATCHES_PURCHASE_INVOICE_LINES_PurchaseInvoiceLineId",
                table: "PRODUCT_BATCHES",
                column: "PurchaseInvoiceLineId",
                principalTable: "PURCHASE_INVOICE_LINES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PURCHASE_INVOICES_EMPLOYEES_CompletedByEmployeeId",
                table: "PURCHASE_INVOICES",
                column: "CompletedByEmployeeId",
                principalTable: "EMPLOYEES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PRODUCT_BATCHES_PURCHASE_INVOICE_LINES_PurchaseInvoiceLineId",
                table: "PRODUCT_BATCHES");

            migrationBuilder.DropForeignKey(
                name: "FK_PURCHASE_INVOICES_EMPLOYEES_CompletedByEmployeeId",
                table: "PURCHASE_INVOICES");

            migrationBuilder.DropIndex(
                name: "IX_PURCHASE_INVOICES_CompletedByEmployeeId",
                table: "PURCHASE_INVOICES");

            migrationBuilder.DropIndex(
                name: "IX_PRODUCT_BATCHES_PurchaseInvoiceLineId",
                table: "PRODUCT_BATCHES");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "PURCHASE_INVOICES");

            migrationBuilder.DropColumn(
                name: "CompletedByEmployeeId",
                table: "PURCHASE_INVOICES");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceLineId",
                table: "PRODUCT_BATCHES");

            migrationBuilder.DropColumn(
                name: "UnitCostUsd",
                table: "PRODUCT_BATCHES");
        }
    }
}
