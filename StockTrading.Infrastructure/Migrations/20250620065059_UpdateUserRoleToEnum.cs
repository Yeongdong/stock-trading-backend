
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
            // 원시 SQL을 사용하여 명시적 변환
            migrationBuilder.Sql(@"
                ALTER TABLE users 
                ALTER COLUMN role TYPE integer 
                USING CASE 
                    WHEN role = 'User' THEN 0
                    WHEN role = 'Admin' THEN 1
                    ELSE 0
                END;
            ");

            // 기본값 설정
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN role SET DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 롤백 시 integer에서 character varying으로 되돌리기
            migrationBuilder.Sql(@"
                ALTER TABLE users 
                ALTER COLUMN role TYPE character varying(50) 
                USING CASE 
                    WHEN role = 0 THEN 'User'
                    WHEN role = 1 THEN 'Admin'
                    ELSE 'User'
                END;
            ");

            // 기본값 설정
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN role SET DEFAULT 'User';");
        }
    }
}