using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    public partial class AddOfferIdToInvoiceLines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OfferId",
                table: "INVOICE_LINES",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_LINES_OfferId",
                table: "INVOICE_LINES",
                column: "OfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_INVOICE_LINES_Offers_OfferId",
                table: "INVOICE_LINES",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INVOICE_LINES_Offers_OfferId",
                table: "INVOICE_LINES");

            migrationBuilder.DropIndex(
                name: "IX_INVOICE_LINES_OfferId",
                table: "INVOICE_LINES");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "INVOICE_LINES");
        }
    }
}
