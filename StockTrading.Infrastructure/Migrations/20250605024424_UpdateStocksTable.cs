using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStocksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockType",
                table: "stocks",
                newName: "stock_type");

            migrationBuilder.RenameColumn(
                name: "ParValue",
                table: "stocks",
                newName: "par_value");

            migrationBuilder.RenameColumn(
                name: "ListedShares",
                table: "stocks",
                newName: "listed_shares");

            migrationBuilder.RenameColumn(
                name: "ListedDate",
                table: "stocks",
                newName: "listed_date");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "stocks",
                newName: "full_name");

            migrationBuilder.AlterColumn<string>(
                name: "stock_type",
                table: "stocks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "par_value",
                table: "stocks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "listed_shares",
                table: "stocks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "stocks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "stock_type",
                table: "stocks",
                newName: "StockType");

            migrationBuilder.RenameColumn(
                name: "par_value",
                table: "stocks",
                newName: "ParValue");

            migrationBuilder.RenameColumn(
                name: "listed_shares",
                table: "stocks",
                newName: "ListedShares");

            migrationBuilder.RenameColumn(
                name: "listed_date",
                table: "stocks",
                newName: "ListedDate");

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "stocks",
                newName: "FullName");

            migrationBuilder.AlterColumn<string>(
                name: "StockType",
                table: "stocks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParValue",
                table: "stocks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ListedShares",
                table: "stocks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "stocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
