using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitSettingsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "APP_SCREENS",
                columns: new[] { "Id", "ScreenKey", "ScreenName" },
                values: new object[,]
                {
                    { 10, "Backup", "النسخ الاحتياطي" },
                    { 11, "AuditLogs", "سجل التدقيق" },
                    { 12, "Employees", "الموظفون" },
                    { 13, "Devices", "الأجهزة" }
                });

            // Backward Compatibility: Grant new permissions to any employee who already has 'Settings' (Id = 7)
            migrationBuilder.Sql(@"
                INSERT INTO EMPLOYEE_SCREEN_PERMISSIONS (EmployeeId, ScreenId, CanAccess)
                SELECT EmployeeId, 10, 1 FROM EMPLOYEE_SCREEN_PERMISSIONS WHERE ScreenId = 7 AND CanAccess = 1;
                
                INSERT INTO EMPLOYEE_SCREEN_PERMISSIONS (EmployeeId, ScreenId, CanAccess)
                SELECT EmployeeId, 11, 1 FROM EMPLOYEE_SCREEN_PERMISSIONS WHERE ScreenId = 7 AND CanAccess = 1;
                
                INSERT INTO EMPLOYEE_SCREEN_PERMISSIONS (EmployeeId, ScreenId, CanAccess)
                SELECT EmployeeId, 12, 1 FROM EMPLOYEE_SCREEN_PERMISSIONS WHERE ScreenId = 7 AND CanAccess = 1;
                
                INSERT INTO EMPLOYEE_SCREEN_PERMISSIONS (EmployeeId, ScreenId, CanAccess)
                SELECT EmployeeId, 13, 1 FROM EMPLOYEE_SCREEN_PERMISSIONS WHERE ScreenId = 7 AND CanAccess = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Backward Compatibility: Remove granted permissions
            migrationBuilder.Sql("DELETE FROM EMPLOYEE_SCREEN_PERMISSIONS WHERE ScreenId IN (10, 11, 12, 13);");

            migrationBuilder.DeleteData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}
