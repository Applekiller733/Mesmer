using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongAppApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Playlists");

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Playlists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlaylistInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistInvitations_Accounts_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaylistInvitations_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaylistInvitations_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistInvitations_PlaylistId_ReceiverId",
                table: "PlaylistInvitations",
                columns: new[] { "PlaylistId", "ReceiverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistInvitations_ReceiverId_CreatedAt",
                table: "PlaylistInvitations",
                columns: new[] { "ReceiverId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistInvitations_SenderId_CreatedAt",
                table: "PlaylistInvitations",
                columns: new[] { "SenderId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistInvitations");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Playlists");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Playlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
