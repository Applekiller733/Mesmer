using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace SongAppApi.Migrations
{
    /// <inheritdoc />
    public partial class RecommendationsRebuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Danceability",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Energy",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Tempo",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Valence",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "MBId",
                table: "Songs",
                newName: "EnrichmentSource");

            migrationBuilder.AddColumn<int>(
                name: "EnrichmentStatus",
                table: "Songs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MusicBrainzId",
                table: "Songs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "PcaFeatures",
                table: "Songs",
                type: "vector(15)",
                nullable: true);

            migrationBuilder.AddColumn<float[]>(
                name: "RawFeatures",
                table: "Songs",
                type: "real[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrichmentStatus",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "MusicBrainzId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "PcaFeatures",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "RawFeatures",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "EnrichmentSource",
                table: "Songs",
                newName: "MBId");

            migrationBuilder.AddColumn<float>(
                name: "Danceability",
                table: "Songs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "Songs",
                type: "vector(128)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Energy",
                table: "Songs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Tempo",
                table: "Songs",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Valence",
                table: "Songs",
                type: "real",
                nullable: true);
        }
    }
}
