using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataBase.Migrations
{
    public partial class Add_Token_ToDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    InvitationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PermissionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ListAggregatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.InvitationId);
                });

            migrationBuilder.CreateTable(
                name: "ListAggregator",
                columns: table => new
                {
                    ListAggregatorId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListAggregatorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListAggregator", x => x.ListAggregatorId);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    log_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    log_level = table.Column<string>(type: "TEXT", nullable: true),
                    source = table.Column<string>(type: "TEXT", nullable: true),
                    message = table.Column<string>(type: "TEXT", nullable: true),
                    exception_message = table.Column<string>(type: "TEXT", nullable: true),
                    stack_trace = table.Column<string>(type: "TEXT", nullable: true),
                    created_date = table.Column<string>(type: "TEXT", nullable: true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: true),
                    inner_id = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_Logs_Logs_inner_id",
                        column: x => x.inner_id,
                        principalTable: "Logs",
                        principalColumn: "log_id");
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "ToDelete",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemToDeleteId = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDelete", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LoginType = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "List",
                columns: table => new
                {
                    ListId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ListAggregatorId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_List", x => x.ListId);
                    table.ForeignKey(
                        name: "FK_List_ListAggregator_ListAggregatorId",
                        column: x => x.ListAggregatorId,
                        principalTable: "ListAggregator",
                        principalColumn: "ListAggregatorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokenSession",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    AccessTokenJti = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRefreshTokenRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviceInfo = table.Column<string>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokenSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokenSession_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserListAggregators",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ListAggregatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserListAggregators", x => new { x.UserId, x.ListAggregatorId });
                    table.ForeignKey(
                        name: "FK_UserListAggregators_ListAggregator_ListAggregatorId",
                        column: x => x.ListAggregatorId,
                        principalTable: "ListAggregator",
                        principalColumn: "ListAggregatorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserListAggregators_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRolesEntity",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRolesEntity", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRolesEntity_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRolesEntity_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListItem",
                columns: table => new
                {
                    ListItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListItemName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    ListId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListItem", x => x.ListItemId);
                    table.ForeignKey(
                        name: "FK_ListItem_List_ListId",
                        column: x => x.ListId,
                        principalTable: "List",
                        principalColumn: "ListId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_EmailAddress",
                table: "Invitations",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_List_ListAggregatorId",
                table: "List",
                column: "ListAggregatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ListItem_ListId",
                table: "ListItem",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_inner_id",
                table: "Logs",
                column: "inner_id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenSession_UserId",
                table: "RefreshTokenSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_EmailAddress",
                table: "User",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserListAggregators_ListAggregatorId",
                table: "UserListAggregators",
                column: "ListAggregatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRolesEntity_RoleId",
                table: "UserRolesEntity",
                column: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "ListItem");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "RefreshTokenSession");

            migrationBuilder.DropTable(
                name: "ToDelete");

            migrationBuilder.DropTable(
                name: "UserListAggregators");

            migrationBuilder.DropTable(
                name: "UserRolesEntity");

            migrationBuilder.DropTable(
                name: "List");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "ListAggregator");
        }
    }
}
