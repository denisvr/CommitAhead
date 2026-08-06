using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCVPresentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cv_presentations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_market = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    locale = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    template_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary_override_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    include_photo = table.Column<bool>(type: "boolean", nullable: false),
                    include_email = table.Column<bool>(type: "boolean", nullable: false),
                    include_phone = table.Column<bool>(type: "boolean", nullable: false),
                    include_address = table.Column<bool>(type: "boolean", nullable: false),
                    date_format = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    page_limit = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    experience_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    education_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    skill_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    language_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    certification_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    project_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    profile_link_selections = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_presentations", x => x.id);
                    table.ForeignKey(
                        name: "FK_cv_presentations_professional_profiles_professional_profile~",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cv_presentations_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_presentations_owner_user_id",
                table: "cv_presentations",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cv_presentations_professional_profile_id",
                table: "cv_presentations",
                column: "professional_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cv_presentations");
        }
    }
}
