using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKisTokensNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KisTokens_users_UserId",
                table: "KisTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KisTokens",
                table: "KisTokens");

            migrationBuilder.RenameTable(
                name: "KisTokens",
                newName: "kis_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_KisTokens_UserId",
                table: "kis_tokens",
                newName: "IX_kis_tokens_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_kis_tokens",
                table: "kis_tokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_kis_tokens_users_UserId",
                table: "kis_tokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kis_tokens_users_UserId",
                table: "kis_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kis_tokens",
                table: "kis_tokens");

            migrationBuilder.RenameTable(
                name: "kis_tokens",
                newName: "KisTokens");

            migrationBuilder.RenameIndex(
                name: "IX_kis_tokens_UserId",
                table: "KisTokens",
                newName: "IX_KisTokens_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KisTokens",
                table: "KisTokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KisTokens_users_UserId",
                table: "KisTokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
