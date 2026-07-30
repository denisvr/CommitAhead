using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scoring_config_overrides",
                columns: table => new
                {
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    importance_weight = table.Column<int>(type: "integer", nullable: false),
                    demand_weight = table.Column<int>(type: "integer", nullable: false),
                    mastery_gap_weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoring_config_overrides", x => x.owner_user_id);
                });

            migrationBuilder.CreateTable(
                name: "study_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    importance = table.Column<int>(type: "integer", nullable: false),
                    initial_mastery = table.Column<int>(type: "integer", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: false),
                    priority_override_score = table.Column<int>(type: "integer", nullable: true),
                    priority_override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confidence_rating = table.Column<int>(type: "integer", nullable: false),
                    notes_markdown = table.Column<string>(type: "text", nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_study_items_owner_user_id_status",
                table: "study_items",
                columns: new[] { "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_study_reviews_study_item_id",
                table: "study_reviews",
                column: "study_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scoring_config_overrides");

            migrationBuilder.DropTable(
                name: "study_reviews");

            migrationBuilder.DropTable(
                name: "study_items");
        }
    }
}
