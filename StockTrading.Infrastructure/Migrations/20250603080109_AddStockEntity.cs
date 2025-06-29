using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stocks",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    english_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    market = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks", x => x.code);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stocks_market",
                table: "stocks",
                column: "market");

            migrationBuilder.CreateIndex(
                name: "ix_stocks_name",
                table: "stocks",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_stocks_sector",
                table: "stocks",
                column: "sector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stocks");
        }
    }
}
