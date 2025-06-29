using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    stock_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    trade_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_orders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_orders_UserId",
                table: "stock_orders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_orders");
        }
    }
}
