using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataBase.Migrations
{
    public partial class Update_Invitaion_ForeighKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_EmailAddress",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "Invitations");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ListAggregatorId",
                table: "Invitations",
                column: "ListAggregatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_ListAggregator_ListAggregatorId",
                table: "Invitations",
                column: "ListAggregatorId",
                principalTable: "ListAggregator",
                principalColumn: "ListAggregatorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_User_UserId",
                table: "Invitations",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_ListAggregator_ListAggregatorId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_User_UserId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_ListAggregatorId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations");

            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "Invitations",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_EmailAddress",
                table: "Invitations",
                column: "EmailAddress",
                unique: true);
        }
    }
}
