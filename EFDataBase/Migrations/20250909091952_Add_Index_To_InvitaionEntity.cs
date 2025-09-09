using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataBase.Migrations
{
    public partial class Add_Index_To_InvitaionEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserId_ListAggregatorId",
                table: "Invitations",
                columns: new[] { "UserId", "ListAggregatorId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_UserId_ListAggregatorId",
                table: "Invitations");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations",
                column: "UserId");
        }
    }
}
