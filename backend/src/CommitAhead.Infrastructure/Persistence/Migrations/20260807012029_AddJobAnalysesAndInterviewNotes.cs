using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAnalysesAndInterviewNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    job_source = table.Column<string>(type: "jsonb", nullable: false),
                    notes_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_analyses_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interview_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    interview_round = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    other_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    questions = table.Column<string[]>(type: "text[]", nullable: false),
                    gaps = table.Column<string[]>(type: "text[]", nullable: false),
                    lessons = table.Column<string[]>(type: "text[]", nullable: false),
                    job_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_interview_notes_job_analyses_job_analysis_id",
                        column: x => x.job_analysis_id,
                        principalTable: "job_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_interview_notes_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_gaps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    job_analysis_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_gaps", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_gaps_job_analyses_job_analysis_id",
                        column: x => x.job_analysis_id,
                        principalTable: "job_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    job_analysis_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_requirements", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_requirements_job_analyses_job_analysis_id",
                        column: x => x.job_analysis_id,
                        principalTable: "job_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_interview_notes_job_analysis_id",
                table: "interview_notes",
                column: "job_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_notes_owner_user_id",
                table: "interview_notes",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_analyses_owner_user_id",
                table: "job_analyses",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_gaps_job_analysis_id",
                table: "job_gaps",
                column: "job_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_gaps_requirement_id",
                table: "job_gaps",
                column: "requirement_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_requirements_job_analysis_id",
                table: "job_requirements",
                column: "job_analysis_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interview_notes");

            migrationBuilder.DropTable(
                name: "job_gaps");

            migrationBuilder.DropTable(
                name: "job_requirements");

            migrationBuilder.DropTable(
                name: "job_analyses");
        }
    }
}
