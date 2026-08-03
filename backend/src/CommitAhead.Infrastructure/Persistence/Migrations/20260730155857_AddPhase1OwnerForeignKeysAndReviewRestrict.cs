using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase1OwnerForeignKeysAndReviewRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evidence_links_study_items_target_study_item_id",
                table: "evidence_links");

            migrationBuilder.DropForeignKey(
                name: "FK_study_reviews_study_items_study_item_id",
                table: "study_reviews");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_study_items_owner_user_id_id",
                table: "study_items",
                columns: new[] { "owner_user_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_links_owner_user_id_target_study_item_id",
                table: "evidence_links",
                columns: new[] { "owner_user_id", "target_study_item_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_evidence_links_study_items_owner_user_id_target_study_item_~",
                table: "evidence_links",
                columns: new[] { "owner_user_id", "target_study_item_id" },
                principalTable: "study_items",
                principalColumns: new[] { "owner_user_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_evidence_links_users_owner_user_id",
                table: "evidence_links",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_scoring_config_overrides_users_owner_user_id",
                table: "scoring_config_overrides",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_study_items_users_owner_user_id",
                table: "study_items",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_study_reviews_study_items_study_item_id",
                table: "study_reviews",
                column: "study_item_id",
                principalTable: "study_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evidence_links_study_items_owner_user_id_target_study_item_~",
                table: "evidence_links");

            migrationBuilder.DropForeignKey(
                name: "FK_evidence_links_users_owner_user_id",
                table: "evidence_links");

            migrationBuilder.DropForeignKey(
                name: "FK_scoring_config_overrides_users_owner_user_id",
                table: "scoring_config_overrides");

            migrationBuilder.DropForeignKey(
                name: "FK_study_items_users_owner_user_id",
                table: "study_items");

            migrationBuilder.DropForeignKey(
                name: "FK_study_reviews_study_items_study_item_id",
                table: "study_reviews");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_study_items_owner_user_id_id",
                table: "study_items");

            migrationBuilder.DropIndex(
                name: "IX_evidence_links_owner_user_id_target_study_item_id",
                table: "evidence_links");

            migrationBuilder.AddForeignKey(
                name: "FK_evidence_links_study_items_target_study_item_id",
                table: "evidence_links",
                column: "target_study_item_id",
                principalTable: "study_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_study_reviews_study_items_study_item_id",
                table: "study_reviews",
                column: "study_item_id",
                principalTable: "study_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
