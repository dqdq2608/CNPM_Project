using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityServerLogic.Migrations.ApplicationDb
{
    public partial class AddRestaurantStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Sửa default cho IsRestaurantActive = true
            migrationBuilder.AlterColumn<bool>(
                name: "IsRestaurantActive",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            // 2) Sửa default cho RestaurantStatus = 'Active'
            migrationBuilder.AlterColumn<string>(
                name: "RestaurantStatus",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // 3) Update dữ liệu cũ: tất cả RestaurantAdmin → active
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers""
                SET ""IsRestaurantActive"" = TRUE,
                    ""RestaurantStatus"" = 'Active'
                WHERE ""UserType"" = 'RestaurantAdmin'
                  AND (""IsRestaurantActive"" = FALSE OR ""IsRestaurantActive"" IS NULL);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nếu muốn undo default thì sửa lại như cũ (không default)
            migrationBuilder.AlterColumn<bool>(
                name: "IsRestaurantActive",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "RestaurantStatus",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Active");
        }
    }
}
