using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCVPresentationSameOwnerConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cv_presentations_professional_profiles_professional_profile~",
                table: "cv_presentations");

            migrationBuilder.DropIndex(
                name: "IX_cv_presentations_professional_profile_id",
                table: "cv_presentations");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_professional_profiles_id_owner_user_id",
                table: "professional_profiles",
                columns: new[] { "id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cv_presentations_professional_profile_id_owner_user_id",
                table: "cv_presentations",
                columns: new[] { "professional_profile_id", "owner_user_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_cv_presentations_professional_profiles_professional_profile~",
                table: "cv_presentations",
                columns: new[] { "professional_profile_id", "owner_user_id" },
                principalTable: "professional_profiles",
                principalColumns: new[] { "id", "owner_user_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cv_presentations_professional_profiles_professional_profile~",
                table: "cv_presentations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_professional_profiles_id_owner_user_id",
                table: "professional_profiles");

            migrationBuilder.DropIndex(
                name: "IX_cv_presentations_professional_profile_id_owner_user_id",
                table: "cv_presentations");

            migrationBuilder.CreateIndex(
                name: "IX_cv_presentations_professional_profile_id",
                table: "cv_presentations",
                column: "professional_profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_cv_presentations_professional_profiles_professional_profile~",
                table: "cv_presentations",
                column: "professional_profile_id",
                principalTable: "professional_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
