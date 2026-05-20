using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CopyCat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramControlOperationRetryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use IF NOT EXISTS so this migration is safe to apply even when the columns
            // were already added by a previous (now-corrected) version of AddPendingChanges.
            migrationBuilder.Sql("""
                ALTER TABLE "TelegramControlOperations"
                    ADD COLUMN IF NOT EXISTS "MaxAttempts"  integer                    NOT NULL DEFAULT 3,
                    ADD COLUMN IF NOT EXISTS "NextRetryAt"  timestamp with time zone       NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "TelegramControlOperations"
                    DROP COLUMN IF EXISTS "MaxAttempts",
                    DROP COLUMN IF EXISTS "NextRetryAt";
                """);
        }
    }
}

