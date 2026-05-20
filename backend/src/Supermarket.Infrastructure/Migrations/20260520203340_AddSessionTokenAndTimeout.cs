using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTokenAndTimeout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "CASH_SESSIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "CASH_SESSIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionToken",
                table: "CASH_SESSIONS",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenCreatedAt",
                table: "CASH_SESSIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CASH_SESSIONS_ExpiresAt",
                table: "CASH_SESSIONS",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CASH_SESSIONS_SessionToken",
                table: "CASH_SESSIONS",
                column: "SessionToken",
                unique: true,
                filter: "[SessionToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CASH_SESSIONS_Status_ExpiresAt",
                table: "CASH_SESSIONS",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CASH_SESSIONS_ExpiresAt",
                table: "CASH_SESSIONS");

            migrationBuilder.DropIndex(
                name: "IX_CASH_SESSIONS_SessionToken",
                table: "CASH_SESSIONS");

            migrationBuilder.DropIndex(
                name: "IX_CASH_SESSIONS_Status_ExpiresAt",
                table: "CASH_SESSIONS");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "CASH_SESSIONS");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "CASH_SESSIONS");

            migrationBuilder.DropColumn(
                name: "SessionToken",
                table: "CASH_SESSIONS");

            migrationBuilder.DropColumn(
                name: "TokenCreatedAt",
                table: "CASH_SESSIONS");
        }
    }
}
