using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkkaDistribution.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Manifests",
                columns: table => new
                {
                    ManifestId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifests", x => x.ManifestId);
                });

            migrationBuilder.CreateTable(
                name: "ManifestEntries",
                columns: table => new
                {
                    ManifestEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: false),
                    FilePiece = table.Column<string>(type: "TEXT", nullable: false),
                    ManifestId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestEntries", x => x.ManifestEntryId);
                    table.ForeignKey(
                        name: "FK_ManifestEntries_Manifests_ManifestId",
                        column: x => x.ManifestId,
                        principalTable: "Manifests",
                        principalColumn: "ManifestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManifestEntries_ManifestId",
                table: "ManifestEntries",
                column: "ManifestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManifestEntries");

            migrationBuilder.DropTable(
                name: "Manifests");
        }
    }
}
