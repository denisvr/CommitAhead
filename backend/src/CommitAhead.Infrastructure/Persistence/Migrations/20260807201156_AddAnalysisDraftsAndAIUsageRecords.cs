using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisDraftsAndAIUsageRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_usage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    command_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pricing_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reserved_input_tokens = table.Column<int>(type: "integer", nullable: false),
                    reserved_output_tokens = table.Column<int>(type: "integer", nullable: false),
                    reserved_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    actual_input_tokens = table.Column<int>(type: "integer", nullable: true),
                    actual_output_tokens = table.Column<int>(type: "integer", nullable: true),
                    actual_cost = table.Column<decimal>(type: "numeric", nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
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
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discarded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "link_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_study_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_weight = table.Column<decimal>(type: "numeric", nullable: false),
                    proposed_rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    accepted_weight = table.Column<decimal>(type: "numeric", nullable: true),
                    accepted_rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    proposed_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    proposed_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    proposed_details = table.Column<string>(type: "jsonb", nullable: false),
                    proposed_tags = table.Column<string[]>(type: "text[]", nullable: false),
                    proposed_importance = table.Column<int>(type: "integer", nullable: false),
                    accepted_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    accepted_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    accepted_details = table.Column<string>(type: "jsonb", nullable: true),
                    accepted_tags = table.Column<string[]>(type: "text[]", nullable: true),
                    accepted_importance = table.Column<int>(type: "integer", nullable: true),
                    accepted_initial_mastery = table.Column<int>(type: "integer", nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    proposed_payload = table.Column<string>(type: "jsonb", nullable: false),
                    accepted_payload = table.Column<string>(type: "jsonb", nullable: true),
                    analysis_draft_id = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_records_idempotency_key",
                table: "ai_usage_records",
                column: "idempotency_key",
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
                name: "IX_suggestion_proposals_analysis_draft_id",
                table: "suggestion_proposals",
                column: "analysis_draft_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_records");

            migrationBuilder.DropTable(
                name: "link_proposals");

            migrationBuilder.DropTable(
                name: "study_item_proposals");

            migrationBuilder.DropTable(
                name: "suggestion_proposals");

            migrationBuilder.DropTable(
                name: "analysis_drafts");
        }
    }
}
