using Microsoft.EntityFrameworkCore.Migrations;

namespace ShoppingListWebApi.Migrations
{
    public partial class Add_State_To_UserListAggregatorEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "UserListAggregators",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "UserListAggregators");
        }
    }
}
