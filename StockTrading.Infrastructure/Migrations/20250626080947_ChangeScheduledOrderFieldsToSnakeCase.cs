using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeScheduledOrderFieldsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduledExecutionTime",
                table: "stock_orders",
                newName: "scheduled_execution_time");

            migrationBuilder.RenameColumn(
                name: "ReservedOrderNumber",
                table: "stock_orders",
                newName: "reserved_order_number");

            migrationBuilder.RenameColumn(
                name: "IsScheduledOrder",
                table: "stock_orders",
                newName: "is_scheduled_order");

            migrationBuilder.AlterColumn<string>(
                name: "reserved_order_number",
                table: "stock_orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_scheduled_order",
                table: "stock_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "scheduled_execution_time",
                table: "stock_orders",
                newName: "ScheduledExecutionTime");

            migrationBuilder.RenameColumn(
                name: "reserved_order_number",
                table: "stock_orders",
                newName: "ReservedOrderNumber");

            migrationBuilder.RenameColumn(
                name: "is_scheduled_order",
                table: "stock_orders",
                newName: "IsScheduledOrder");

            migrationBuilder.AlterColumn<string>(
                name: "ReservedOrderNumber",
                table: "stock_orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsScheduledOrder",
                table: "stock_orders",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
