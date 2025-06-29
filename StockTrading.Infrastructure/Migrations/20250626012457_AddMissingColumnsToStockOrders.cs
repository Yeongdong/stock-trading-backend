using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToStockOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 기존 외래키 제약조건 삭제
            migrationBuilder.DropForeignKey(
                name: "stock_orders_UserId_fkey",
                table: "stock_orders");

            // Market 컬럼 추가
            migrationBuilder.AddColumn<string>(
                name: "market",
                table: "stock_orders",
                type: "text",
                nullable: false,
                defaultValue: "Kospi");

            // Currency 컬럼 추가
            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "stock_orders",
                type: "text",
                nullable: false,
                defaultValue: "Krw");

            // UserId 컬럼명을 user_id로 변경
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "stock_orders",
                newName: "user_id");

            // stock_code 컬럼 길이 확장 (6 -> 10)
            migrationBuilder.AlterColumn<string>(
                name: "stock_code",
                table: "stock_orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6);

            // 새로운 외래키 제약조건 생성
            migrationBuilder.AddForeignKey(
                name: "FK_stock_orders_users_user_id",
                table: "stock_orders",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 새로운 외래키 제약조건 삭제
            migrationBuilder.DropForeignKey(
                name: "FK_stock_orders_users_user_id",
                table: "stock_orders");

            // Market 컬럼 삭제
            migrationBuilder.DropColumn(
                name: "market",
                table: "stock_orders");

            // Currency 컬럼 삭제
            migrationBuilder.DropColumn(
                name: "currency",
                table: "stock_orders");

            // user_id 컬럼명을 UserId로 되돌리기
            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "stock_orders",
                newName: "UserId");

            // stock_code 컬럼 길이 원복 (10 -> 6)
            migrationBuilder.AlterColumn<string>(
                name: "stock_code",
                table: "stock_orders",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            // 기존 외래키 제약조건 복원
            migrationBuilder.AddForeignKey(
                name: "stock_orders_UserId_fkey",
                table: "stock_orders",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}