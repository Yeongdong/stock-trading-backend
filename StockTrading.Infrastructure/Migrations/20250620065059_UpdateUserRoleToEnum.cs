using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRoleToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Role 컬럼을 character varying에서 integer로 변환
            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: false,
                oldDefaultValue: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 롤백 시 integer에서 character varying으로 되돌리기
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "User",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: false,
                oldDefaultValue: 0);
        }
    }
}