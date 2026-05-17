using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitPosDevicesCashSessionsAndAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_SETTINGS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByEmployeeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_SETTINGS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APP_SETTINGS_EMPLOYEES_UpdatedByEmployeeId",
                        column: x => x.UpdatedByEmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "POS_DEVICES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_DEVICES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CASH_SESSIONS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASH_SESSIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CASH_SESSIONS_EMPLOYEES_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CASH_SESSIONS_POS_DEVICES_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "POS_DEVICES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "APP_SETTINGS",
                columns: new[] { "Id", "SettingKey", "SettingValue", "UpdatedAt", "UpdatedByEmployeeId" },
                values: new object[,]
                {
                    { 1L, "exchange_rate_syp", "15000", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2L, "stock_alert_threshold", "10", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3L, "expiry_alert_days", "30", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_APP_SETTINGS_SettingKey",
                table: "APP_SETTINGS",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_APP_SETTINGS_UpdatedByEmployeeId",
                table: "APP_SETTINGS",
                column: "UpdatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CASH_SESSIONS_DeviceId",
                table: "CASH_SESSIONS",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_CASH_SESSIONS_EmployeeId_Status",
                table: "CASH_SESSIONS",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_POS_DEVICES_DeviceCode",
                table: "POS_DEVICES",
                column: "DeviceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APP_SETTINGS");

            migrationBuilder.DropTable(
                name: "CASH_SESSIONS");

            migrationBuilder.DropTable(
                name: "POS_DEVICES");
        }
    }
}
