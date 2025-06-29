using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeKisTokenTableConventionToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenType",
                table: "kis_tokens",
                newName: "token_type");

            migrationBuilder.RenameColumn(
                name: "ExpiresIn",
                table: "kis_tokens",
                newName: "expires_in");

            migrationBuilder.RenameColumn(
                name: "AccessToken",
                table: "kis_tokens",
                newName: "access_token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token_type",
                table: "kis_tokens",
                newName: "TokenType");

            migrationBuilder.RenameColumn(
                name: "expires_in",
                table: "kis_tokens",
                newName: "ExpiresIn");

            migrationBuilder.RenameColumn(
                name: "access_token",
                table: "kis_tokens",
                newName: "AccessToken");
        }
    }
}
