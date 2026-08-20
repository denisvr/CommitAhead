using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalProfileEntryPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "project_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "experience_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "education_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "certification_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "position",
                table: "project_entries");

            migrationBuilder.DropColumn(
                name: "position",
                table: "experience_entries");

            migrationBuilder.DropColumn(
                name: "position",
                table: "education_entries");

            migrationBuilder.DropColumn(
                name: "position",
                table: "certification_entries");
        }
    }
}
