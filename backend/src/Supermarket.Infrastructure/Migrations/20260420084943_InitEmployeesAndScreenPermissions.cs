using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitEmployeesAndScreenPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_SCREENS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScreenKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScreenName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_SCREENS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EMPLOYEES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLOYEES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EMPLOYEE_SCREEN_PERMISSIONS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ScreenId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLOYEE_SCREEN_PERMISSIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMPLOYEE_SCREEN_PERMISSIONS_APP_SCREENS_ScreenId",
                        column: x => x.ScreenId,
                        principalTable: "APP_SCREENS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EMPLOYEE_SCREEN_PERMISSIONS_EMPLOYEES_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "APP_SCREENS",
                columns: new[] { "Id", "ScreenKey", "ScreenName" },
                values: new object[,]
                {
                    { 1, "sales", "Sales" },
                    { 2, "products", "Products" },
                    { 3, "invoices", "Invoices" },
                    { 4, "offers", "Offers" },
                    { 5, "reports", "Reports" },
                    { 6, "inventory", "Inventory" },
                    { 7, "settings", "Settings" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_APP_SCREENS_ScreenKey",
                table: "APP_SCREENS",
                column: "ScreenKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEE_SCREEN_PERMISSIONS_EmployeeId_ScreenId",
                table: "EMPLOYEE_SCREEN_PERMISSIONS",
                columns: new[] { "EmployeeId", "ScreenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEE_SCREEN_PERMISSIONS_ScreenId",
                table: "EMPLOYEE_SCREEN_PERMISSIONS",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEES_Username",
                table: "EMPLOYEES",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMPLOYEE_SCREEN_PERMISSIONS");

            migrationBuilder.DropTable(
                name: "APP_SCREENS");

            migrationBuilder.DropTable(
                name: "EMPLOYEES");
        }
    }
}
