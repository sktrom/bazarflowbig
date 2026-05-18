using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RuntimeStabilizationPatch01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OfferId",
                table: "INVOICE_LINES",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 1,
                column: "ScreenKey",
                value: "Sales");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 2,
                column: "ScreenKey",
                value: "Products");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 3,
                column: "ScreenKey",
                value: "Invoices");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 4,
                column: "ScreenKey",
                value: "Offers");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 5,
                column: "ScreenKey",
                value: "Reports");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 6,
                column: "ScreenKey",
                value: "Inventory");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 7,
                column: "ScreenKey",
                value: "Settings");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM POS_DEVICES WHERE DeviceCode = 'DEFAULT_DEVICE')
BEGIN
    INSERT INTO POS_DEVICES (DeviceCode, DeviceName, IsActive)
    VALUES ('DEFAULT_DEVICE', 'Default POS Device', 1);
END

IF NOT EXISTS (SELECT 1 FROM EMPLOYEES WHERE Username = 'admin')
BEGIN
    INSERT INTO EMPLOYEES (FullName, Username, Phone, PasswordHash, IsActive, CreatedAt, UpdatedAt)
    VALUES ('System Admin', 'admin', '', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 1, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');
END

DECLARE @AdminId BIGINT;
SELECT @AdminId = Id FROM EMPLOYEES WHERE Username = 'admin';

IF @AdminId IS NOT NULL
BEGIN
    DECLARE @Screens TABLE (ScreenKey NVARCHAR(50));
    INSERT INTO @Screens VALUES ('Sales'), ('Products'), ('Invoices'), ('Offers'), ('Reports'), ('Inventory'), ('Settings');

    DECLARE @ScreenId INT;
    DECLARE @CurrentKey NVARCHAR(50);
    
    DECLARE screen_cursor CURSOR FOR SELECT ScreenKey FROM @Screens;
    OPEN screen_cursor;
    FETCH NEXT FROM screen_cursor INTO @CurrentKey;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @ScreenId = Id FROM APP_SCREENS WHERE ScreenKey = @CurrentKey;
        
        IF @ScreenId IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM EMPLOYEE_SCREEN_PERMISSIONS WHERE EmployeeId = @AdminId AND ScreenId = @ScreenId)
            BEGIN
                INSERT INTO EMPLOYEE_SCREEN_PERMISSIONS (EmployeeId, ScreenId, CanAccess)
                VALUES (@AdminId, @ScreenId, 1);
            END
            ELSE
            BEGIN
                UPDATE EMPLOYEE_SCREEN_PERMISSIONS
                SET CanAccess = 1
                WHERE EmployeeId = @AdminId AND ScreenId = @ScreenId;
            END
        END
        
        FETCH NEXT FROM screen_cursor INTO @CurrentKey;
    END;
    CLOSE screen_cursor;
    DEALLOCATE screen_cursor;
END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_LINES_OfferId",
                table: "INVOICE_LINES",
                column: "OfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_INVOICE_LINES_OFFERS_OfferId",
                table: "INVOICE_LINES",
                column: "OfferId",
                principalTable: "OFFERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INVOICE_LINES_OFFERS_OfferId",
                table: "INVOICE_LINES");

            migrationBuilder.DropIndex(
                name: "IX_INVOICE_LINES_OfferId",
                table: "INVOICE_LINES");

            // Down migration for seeded data is skipped to avoid destructive deletes.
            // Admin user, default device, and permissions will remain intact.

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "INVOICE_LINES");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 1,
                column: "ScreenKey",
                value: "sales");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 2,
                column: "ScreenKey",
                value: "products");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 3,
                column: "ScreenKey",
                value: "invoices");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 4,
                column: "ScreenKey",
                value: "offers");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 5,
                column: "ScreenKey",
                value: "reports");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 6,
                column: "ScreenKey",
                value: "inventory");

            migrationBuilder.UpdateData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 7,
                column: "ScreenKey",
                value: "settings");
        }
    }
}
