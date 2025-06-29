using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignStocksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "foreign_stocks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    exchange = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    mic = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_foreign_stocks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_foreign_stocks_description",
                table: "foreign_stocks",
                column: "description");

            migrationBuilder.CreateIndex(
                name: "ix_foreign_stocks_exchange",
                table: "foreign_stocks",
                column: "exchange");

            migrationBuilder.CreateIndex(
                name: "ix_foreign_stocks_symbol",
                table: "foreign_stocks",
                column: "symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "foreign_stocks");
        }
    }
}
