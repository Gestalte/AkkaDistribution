using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkkaDistribution.Client.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileParts",
                columns: table => new
                {
                    FilePartId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPieces = table.Column<int>(type: "INTEGER", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileParts", x => x.FilePartId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileParts");
        }
    }
}
