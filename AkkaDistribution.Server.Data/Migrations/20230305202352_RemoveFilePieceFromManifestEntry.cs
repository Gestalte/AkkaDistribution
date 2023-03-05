using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkkaDistribution.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFilePieceFromManifestEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePiece",
                table: "ManifestEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePiece",
                table: "ManifestEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
