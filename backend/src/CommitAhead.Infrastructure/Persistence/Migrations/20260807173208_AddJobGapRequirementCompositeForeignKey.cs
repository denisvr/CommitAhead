using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobGapRequirementCompositeForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_gaps_requirement_id",
                table: "job_gaps");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_job_requirements_id_job_analysis_id",
                table: "job_requirements",
                columns: new[] { "id", "job_analysis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_job_gaps_requirement_id_job_analysis_id",
                table: "job_gaps",
                columns: new[] { "requirement_id", "job_analysis_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_job_gaps_job_requirements_requirement_id_job_analysis_id",
                table: "job_gaps",
                columns: new[] { "requirement_id", "job_analysis_id" },
                principalTable: "job_requirements",
                principalColumns: new[] { "id", "job_analysis_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_gaps_job_requirements_requirement_id_job_analysis_id",
                table: "job_gaps");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_job_requirements_id_job_analysis_id",
                table: "job_requirements");

            migrationBuilder.DropIndex(
                name: "IX_job_gaps_requirement_id_job_analysis_id",
                table: "job_gaps");

            migrationBuilder.CreateIndex(
                name: "IX_job_gaps_requirement_id",
                table: "job_gaps",
                column: "requirement_id");
        }
    }
}
