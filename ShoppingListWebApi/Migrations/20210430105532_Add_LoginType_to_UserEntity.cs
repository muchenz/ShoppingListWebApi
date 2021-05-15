using Microsoft.EntityFrameworkCore.Migrations;

namespace ShoppingListWebApi.Migrations
{
    public partial class Add_LoginType_to_UserEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "LoginType",
                table: "User",
                nullable: false,
                defaultValue: (byte)1)
                .Annotation("Sqlite:Autoincrement", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginType",
                table: "User");
        }
    }
}
