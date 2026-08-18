using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropStudyJobAnalysesInterviewNotesAnalysisDraftsAndAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_records");

            migrationBuilder.DropTable(
                name: "evidence_links");

            migrationBuilder.DropTable(
                name: "interview_notes");

            migrationBuilder.DropTable(
                name: "job_gaps");

            migrationBuilder.DropTable(
                name: "link_proposals");

            migrationBuilder.DropTable(
                name: "scoring_config_overrides");

            migrationBuilder.DropTable(
                name: "study_item_proposals");

            migrationBuilder.DropTable(
                name: "study_reviews");

            migrationBuilder.DropTable(
                name: "suggestion_proposals");

            migrationBuilder.DropTable(
                name: "job_requirements");

            migrationBuilder.DropTable(
                name: "study_items");

            migrationBuilder.DropTable(
                name: "analysis_drafts");

            migrationBuilder.DropTable(
                name: "job_analyses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_usage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_cost = table.Column<decimal>(type: "numeric", nullable: true),
                    actual_input_tokens = table.Column<int>(type: "integer", nullable: true),
                    actual_output_tokens = table.Column<int>(type: "integer", nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    command_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    outcome_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricing_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reserved_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    reserved_input_tokens = table.Column<int>(type: "integer", nullable: false),
                    reserved_output_tokens = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_usage_records_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "analysis_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    discarded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_drafts", x => x.id);
                    table.ForeignKey(
                        name: "FK_analysis_drafts_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    job_source = table.Column<string>(type: "jsonb", nullable: false),
                    notes_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                name: "scoring_config_overrides",
                columns: table => new
                {
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    demand_weight = table.Column<int>(type: "integer", nullable: false),
                    importance_weight = table.Column<int>(type: "integer", nullable: false),
                    mastery_gap_weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoring_config_overrides", x => x.owner_user_id);
                    table.ForeignKey(
                        name: "FK_scoring_config_overrides_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "study_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: false),
                    importance = table.Column<int>(type: "integer", nullable: false),
                    initial_mastery = table.Column<int>(type: "integer", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    priority_override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    priority_override_score = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_items", x => x.id);
                    table.UniqueConstraint("AK_study_items_owner_user_id_id", x => new { x.owner_user_id, x.id });
                    table.ForeignKey(
                        name: "FK_study_items_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "link_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    accepted_weight = table.Column<decimal>(type: "numeric", nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    proposed_weight = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_study_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_link_proposals_analysis_drafts_analysis_draft_id",
                        column: x => x.analysis_draft_id,
                        principalTable: "analysis_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_item_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    accepted_details = table.Column<string>(type: "jsonb", nullable: true),
                    accepted_importance = table.Column<int>(type: "integer", nullable: true),
                    accepted_initial_mastery = table.Column<int>(type: "integer", nullable: true),
                    accepted_tags = table.Column<string[]>(type: "text[]", nullable: true),
                    accepted_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    proposed_details = table.Column<string>(type: "jsonb", nullable: false),
                    proposed_importance = table.Column<int>(type: "integer", nullable: false),
                    proposed_tags = table.Column<string[]>(type: "text[]", nullable: false),
                    proposed_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_item_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_study_item_proposals_analysis_drafts_analysis_draft_id",
                        column: x => x.analysis_draft_id,
                        principalTable: "analysis_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suggestion_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_payload = table.Column<string>(type: "jsonb", nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suggestion_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_suggestion_proposals_analysis_drafts_analysis_draft_id",
                        column: x => x.analysis_draft_id,
                        principalTable: "analysis_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    gaps = table.Column<string[]>(type: "text[]", nullable: false),
                    interview_round = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    job_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lessons = table.Column<string[]>(type: "text[]", nullable: false),
                    other_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    questions = table.Column<string[]>(type: "text[]", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
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
                name: "job_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_requirements", x => x.id);
                    table.UniqueConstraint("AK_job_requirements_id_job_analysis_id", x => new { x.id, x.job_analysis_id });
                    table.ForeignKey(
                        name: "FK_job_requirements_job_analyses_job_analysis_id",
                        column: x => x.job_analysis_id,
                        principalTable: "job_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_study_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_links_study_items_owner_user_id_target_study_item_~",
                        columns: x => new { x.owner_user_id, x.target_study_item_id },
                        principalTable: "study_items",
                        principalColumns: new[] { "owner_user_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_links_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "study_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence_rating = table.Column<int>(type: "integer", nullable: false),
                    notes_markdown = table.Column<string>(type: "text", nullable: true),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    study_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_study_reviews_study_items_study_item_id",
                        column: x => x.study_item_id,
                        principalTable: "study_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_gaps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_job_gaps_job_requirements_requirement_id_job_analysis_id",
                        columns: x => new { x.requirement_id, x.job_analysis_id },
                        principalTable: "job_requirements",
                        principalColumns: new[] { "id", "job_analysis_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_records_owner_user_id",
                table: "ai_usage_records",
                column: "owner_user_id",
                unique: true,
                filter: "status = 'Reserved'");

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_records_owner_user_id_idempotency_key",
                table: "ai_usage_records",
                columns: new[] { "owner_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_records_owner_user_id_started_at_utc",
                table: "ai_usage_records",
                columns: new[] { "owner_user_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_drafts_owner_user_id",
                table: "analysis_drafts",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_drafts_source_type_source_id",
                table: "analysis_drafts",
                columns: new[] { "source_type", "source_id" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_links_owner_user_id_target_study_item_id",
                table: "evidence_links",
                columns: new[] { "owner_user_id", "target_study_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_links_source_type_source_id_target_study_item_id",
                table: "evidence_links",
                columns: new[] { "source_type", "source_id", "target_study_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_links_target_study_item_id",
                table: "evidence_links",
                column: "target_study_item_id");

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
                name: "IX_job_gaps_requirement_id_job_analysis_id",
                table: "job_gaps",
                columns: new[] { "requirement_id", "job_analysis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_job_requirements_job_analysis_id",
                table: "job_requirements",
                column: "job_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_link_proposals_analysis_draft_id",
                table: "link_proposals",
                column: "analysis_draft_id");

            migrationBuilder.CreateIndex(
                name: "IX_link_proposals_target_study_item_id",
                table: "link_proposals",
                column: "target_study_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_item_proposals_analysis_draft_id",
                table: "study_item_proposals",
                column: "analysis_draft_id");

            migrationBuilder.CreateIndex(
                name: "IX_study_items_owner_user_id_status",
                table: "study_items",
                columns: new[] { "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_study_reviews_study_item_id",
                table: "study_reviews",
                column: "study_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_proposals_analysis_draft_id",
                table: "suggestion_proposals",
                column: "analysis_draft_id");
        }
    }
}
