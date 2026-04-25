using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongAppApi.Migrations
{
    /// <inheritdoc />
    public partial class ExplicitForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SpotifyId",
                table: "Songs",
                newName: "MBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MBId",
                table: "Songs",
                newName: "SpotifyId");
        }
    }
}
