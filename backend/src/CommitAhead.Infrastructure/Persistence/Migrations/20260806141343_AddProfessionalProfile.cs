using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "professional_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_info = table.Column<string>(type: "jsonb", nullable: false),
                    summary_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_professional_profiles_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "certification_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    issuing_organisation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    issued_at = table.Column<int>(type: "integer", nullable: true),
                    expires_at = table.Column<int>(type: "integer", nullable: true),
                    credential_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certification_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_certification_entries_professional_profiles_professional_pr~",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "education_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    degree = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    start_date = table.Column<int>(type: "integer", nullable: true),
                    end_date = table.Column<int>(type: "integer", nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    details_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_education_entries_professional_profiles_professional_profil~",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    start_date = table.Column<int>(type: "integer", nullable: false),
                    end_date = table.Column<int>(type: "integer", nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    work_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    summary_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    achievements = table.Column<string[]>(type: "text[]", nullable: false),
                    skill_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_experience_entries_professional_profiles_professional_profi~",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "language_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    proficiency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    certification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_language_entries_professional_profiles_professional_profile~",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_profile_links_professional_profiles_professional_profile_id",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    start_date = table.Column<int>(type: "integer", nullable: true),
                    end_date = table.Column<int>(type: "integer", nullable: true),
                    description_markdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    skill_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_entries_professional_profiles_professional_profile_~",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    proficiency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    professional_profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_skills_professional_profiles_professional_profile_id",
                        column: x => x.professional_profile_id,
                        principalTable: "professional_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_certification_entries_professional_profile_id",
                table: "certification_entries",
                column: "professional_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_education_entries_professional_profile_id",
                table: "education_entries",
                column: "professional_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_experience_entries_professional_profile_id",
                table: "experience_entries",
                column: "professional_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_language_entries_professional_profile_id",
                table: "language_entries",
                column: "professional_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_professional_profiles_owner_user_id",
                table: "professional_profiles",
                column: "owner_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_links_professional_profile_id",
                table: "profile_links",
                column: "professional_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_entries_professional_profile_id",
                table: "project_entries",
                column: "professional_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_skills_professional_profile_id_normalized_key",
                table: "skills",
                columns: new[] { "professional_profile_id", "normalized_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certification_entries");

            migrationBuilder.DropTable(
                name: "education_entries");

            migrationBuilder.DropTable(
                name: "experience_entries");

            migrationBuilder.DropTable(
                name: "language_entries");

            migrationBuilder.DropTable(
                name: "profile_links");

            migrationBuilder.DropTable(
                name: "project_entries");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "professional_profiles");
        }
    }
}
