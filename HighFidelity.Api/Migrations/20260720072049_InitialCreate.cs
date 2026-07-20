using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighFidelity.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Baseline migration — the database already exists from Api/database/seed.sql
        /// so the Up() method is intentionally empty. On a NEW database, run
        ///   dotnet ef database update --connection "..."
        /// which will apply this migration (no-op) and seed the __EFMigrationsHistory table.
        /// Future schema changes will produce normal migrations that run after this baseline.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema already exists via seed.sql. No DDL needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all tables to roll back to empty DB.
        }
    }
}
