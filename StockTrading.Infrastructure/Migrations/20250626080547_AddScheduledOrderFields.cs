using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScheduledOrder",
                table: "stock_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReservedOrderNumber",
                table: "stock_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledExecutionTime",
                table: "stock_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsScheduledOrder",
                table: "stock_orders");

            migrationBuilder.DropColumn(
                name: "ReservedOrderNumber",
                table: "stock_orders");

            migrationBuilder.DropColumn(
                name: "ScheduledExecutionTime",
                table: "stock_orders");
        }
    }
}
