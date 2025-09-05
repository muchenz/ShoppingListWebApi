using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataBase.Migrations
{
    public partial class Add_UserId_ti_Invitaion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Invitations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Invitations");
        }
    }
}
