using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStocksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "stocks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ListedDate",
                table: "stocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListedShares",
                table: "stocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParValue",
                table: "stocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockType",
                table: "stocks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "ListedDate",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "ListedShares",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "ParValue",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "StockType",
                table: "stocks");
        }
    }
}
