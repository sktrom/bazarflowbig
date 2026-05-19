using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "POS_DEVICES",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "POS_DEVICES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "POS_DEVICES",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "POS_DEVICES",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "POS_DEVICES",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreatedAt", "LastLoginAt", "Notes", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, new DateTime(2026, 4, 20, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "POS_DEVICES");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "POS_DEVICES");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "POS_DEVICES");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "POS_DEVICES");
        }
    }
}
