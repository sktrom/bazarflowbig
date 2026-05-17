using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAdjustmentRequestsAndLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADJUSTMENT_REQUESTS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewedByEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    RequestType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADJUSTMENT_REQUESTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADJUSTMENT_REQUESTS_EMPLOYEES_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ADJUSTMENT_REQUESTS_EMPLOYEES_ReviewedByEmployeeId",
                        column: x => x.ReviewedByEmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ADJUSTMENT_REQUESTS_INVOICES_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "INVOICES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ADJUSTMENT_REQUEST_LINES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdjustmentRequestId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceLineId = table.Column<long>(type: "bigint", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    RequestedLineTotalUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADJUSTMENT_REQUEST_LINES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADJUSTMENT_REQUEST_LINES_ADJUSTMENT_REQUESTS_AdjustmentRequestId",
                        column: x => x.AdjustmentRequestId,
                        principalTable: "ADJUSTMENT_REQUESTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ADJUSTMENT_REQUEST_LINES_INVOICE_LINES_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalTable: "INVOICE_LINES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADJUSTMENT_REQUEST_LINES_AdjustmentRequestId",
                table: "ADJUSTMENT_REQUEST_LINES",
                column: "AdjustmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ADJUSTMENT_REQUEST_LINES_InvoiceLineId",
                table: "ADJUSTMENT_REQUEST_LINES",
                column: "InvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ADJUSTMENT_REQUESTS_InvoiceId_Status",
                table: "ADJUSTMENT_REQUESTS",
                columns: new[] { "InvoiceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ADJUSTMENT_REQUESTS_RequestedByEmployeeId",
                table: "ADJUSTMENT_REQUESTS",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ADJUSTMENT_REQUESTS_ReviewedByEmployeeId",
                table: "ADJUSTMENT_REQUESTS",
                column: "ReviewedByEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADJUSTMENT_REQUEST_LINES");

            migrationBuilder.DropTable(
                name: "ADJUSTMENT_REQUESTS");
        }
    }
}
