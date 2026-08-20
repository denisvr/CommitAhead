using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Corrects <c>20260820112709_AddProfessionalProfileEntryPositions</c>: that migration's
    /// default value of 0 left every pre-existing row tied at position 0 within its own profile —
    /// there was no backfill statement despite what persistence.md used to claim (now corrected).
    /// This assigns each affected profile's rows a distinct, deterministic order using Postgres's
    /// own <c>ctid</c> (physical row order) as the tiebreaker — there is no other column recording
    /// the order those rows were originally entered in, so this is a best-effort repair, not a
    /// recovery of the user's actual original intent, which was never persisted. It only touches a
    /// (professional_profile_id, table) group that is BOTH multi-row AND still entirely at
    /// position 0 — a group with any non-zero position already reflects a real
    /// <c>ProfessionalProfile.Replace*</c> save and must be left alone.
    /// </summary>
    public partial class BackfillProfessionalProfileEntryPositions : Migration
    {
        private static readonly string[] AffectedTables =
        [
            "experience_entries",
            "education_entries",
            "certification_entries",
            "project_entries",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in AffectedTables)
            {
                migrationBuilder.Sql($"""
                    WITH untouched_groups AS (
                        SELECT professional_profile_id
                        FROM {table}
                        GROUP BY professional_profile_id
                        HAVING COUNT(*) > 1 AND bool_and(position = 0)
                    ),
                    ranked AS (
                        SELECT t.id, ROW_NUMBER() OVER (PARTITION BY t.professional_profile_id ORDER BY t.ctid) - 1 AS new_position
                        FROM {table} t
                        JOIN untouched_groups g ON g.professional_profile_id = t.professional_profile_id
                    )
                    UPDATE {table} t
                    SET position = ranked.new_position
                    FROM ranked
                    WHERE t.id = ranked.id;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately a no-op: reverting these positions back to all-zero would only
            // reintroduce the exact bug this migration fixes. A schema rollback of this migration
            // still works (the position column itself was added by the prior migration, not this
            // one) — there is simply nothing here to undo.
        }
    }
}
