using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginAttemptsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsernameNormalized = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLoginAttempts_CreatedAtUtc",
                table: "AppLoginAttempts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppLoginAttempts_UsernameNormalized_IpAddress_CreatedAtUtc",
                table: "AppLoginAttempts",
                columns: new[] { "UsernameNormalized", "IpAddress", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLoginAttempts");
        }
    }
}
