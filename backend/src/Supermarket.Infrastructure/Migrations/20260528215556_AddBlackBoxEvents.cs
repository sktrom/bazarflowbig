using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlackBoxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BLACK_BOX_EVENTS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    SessionId = table.Column<long>(type: "bigint", nullable: true),
                    DeviceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PageName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ElementKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataTruncated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLACK_BOX_EVENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BLACK_BOX_EVENTS_CASH_SESSIONS_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CASH_SESSIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BLACK_BOX_EVENTS_EMPLOYEES_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMPLOYEES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "APP_SCREENS",
                columns: new[] { "Id", "ScreenKey", "ScreenName" },
                values: new object[] { 9, "BlackBox", "الصندوق الأسود" });

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_ActionType_CreatedAtUtc",
                table: "BLACK_BOX_EVENTS",
                columns: new[] { "ActionType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_CreatedAtUtc",
                table: "BLACK_BOX_EVENTS",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_DeviceCode_CreatedAtUtc",
                table: "BLACK_BOX_EVENTS",
                columns: new[] { "DeviceCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_EmployeeId_CreatedAtUtc",
                table: "BLACK_BOX_EVENTS",
                columns: new[] { "EmployeeId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_EntityType_EntityId",
                table: "BLACK_BOX_EVENTS",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_PageName_CreatedAtUtc",
                table: "BLACK_BOX_EVENTS",
                columns: new[] { "PageName", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BLACK_BOX_EVENTS_SessionId_CreatedAtUtc",
                table: "BLACK_BOX_EVENTS",
                columns: new[] { "SessionId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BLACK_BOX_EVENTS");

            migrationBuilder.DeleteData(
                table: "APP_SCREENS",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
