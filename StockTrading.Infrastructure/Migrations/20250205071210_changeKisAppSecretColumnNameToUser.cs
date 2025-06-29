using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changeKisAppSecretColumnNameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KisAppSecret",
                table: "users",
                newName: "kis_app_secret");

            migrationBuilder.RenameColumn(
                name: "KisAppKey",
                table: "users",
                newName: "kis_app_key");

            migrationBuilder.AlterColumn<string>(
                name: "kis_app_secret",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "kis_app_key",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "kis_app_secret",
                table: "users",
                newName: "KisAppSecret");

            migrationBuilder.RenameColumn(
                name: "kis_app_key",
                table: "users",
                newName: "KisAppKey");

            migrationBuilder.AlterColumn<string>(
                name: "KisAppSecret",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KisAppKey",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
