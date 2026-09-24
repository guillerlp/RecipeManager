using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Infrastructure.Migrations
{
    // Deliberately empty: RecipeConfiguration now marks Recipe.Id and the owned Ingredient.Id as
    // ValueGeneratedNever(), because both are minted by the domain (Guid.NewGuid() in each entity's
    // private constructor), never by the database. A uuid column with no database-side default
    // produces identical DDL either way, so there is no schema change here -- this migration exists
    // only to bring AppDbContextModelSnapshot.cs back in sync with that corrected metadata, so the
    // next `dotnet ef migrations add` starts from an accurate snapshot instead of silently bundling
    // this diff into an unrelated migration.
    /// <inheritdoc />
    public partial class SyncIdValueGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
