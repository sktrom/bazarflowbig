using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supermarket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAppLoginAttemptsToLoginAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppLoginAttempts",
                table: "AppLoginAttempts");

            migrationBuilder.RenameTable(
                name: "AppLoginAttempts",
                newName: "LOGIN_ATTEMPTS");

            migrationBuilder.RenameIndex(
                name: "IX_AppLoginAttempts_UsernameNormalized_IpAddress_CreatedAtUtc",
                table: "LOGIN_ATTEMPTS",
                newName: "IX_LOGIN_ATTEMPTS_UsernameNormalized_IpAddress_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_AppLoginAttempts_CreatedAtUtc",
                table: "LOGIN_ATTEMPTS",
                newName: "IX_LOGIN_ATTEMPTS_CreatedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LOGIN_ATTEMPTS",
                table: "LOGIN_ATTEMPTS",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LOGIN_ATTEMPTS",
                table: "LOGIN_ATTEMPTS");

            migrationBuilder.RenameTable(
                name: "LOGIN_ATTEMPTS",
                newName: "AppLoginAttempts");

            migrationBuilder.RenameIndex(
                name: "IX_LOGIN_ATTEMPTS_UsernameNormalized_IpAddress_CreatedAtUtc",
                table: "AppLoginAttempts",
                newName: "IX_AppLoginAttempts_UsernameNormalized_IpAddress_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_LOGIN_ATTEMPTS_CreatedAtUtc",
                table: "AppLoginAttempts",
                newName: "IX_AppLoginAttempts_CreatedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppLoginAttempts",
                table: "AppLoginAttempts",
                column: "Id");
        }
    }
}
