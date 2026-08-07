using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommitAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAIUsageRecordIndexesForOwnerScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ai_usage_records_idempotency_key",
                table: "ai_usage_records");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ai_usage_records_owner_user_id",
                table: "ai_usage_records");

            migrationBuilder.DropIndex(
                name: "IX_ai_usage_records_owner_user_id_idempotency_key",
                table: "ai_usage_records");

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_records_idempotency_key",
                table: "ai_usage_records",
                column: "idempotency_key",
                unique: true);
        }
    }
}
