using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Infrastructure.Migrations
{
    // Non-destructive (R-19, ADR-025): relaxes NOT NULL on four columns and adds Status. Down is guarded and
    // refuses to run while any draft exists.
    /// <inheritdoc />
    public partial class AddRecipeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Servings",
                table: "Recipes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "PreparationTime",
                table: "Recipes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Recipes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "CookingTime",
                table: "Recipes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Back-fills every existing row: they already satisfy the full invariants (spec 013). The model has
            // no default (see RecipeConfiguration), so new rows always carry an explicit status.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Recipes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The scaffolded Down replaces nulls with 0 and '' before restoring NOT NULL, so without this guard a
            // rollback would succeed and silently turn every draft into a "complete" recipe with 0 servings, an
            // empty description and no status — data the published invariants reject. Refuse instead.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Recipes" WHERE "Status" = 'Draft') THEN
                        RAISE EXCEPTION 'AddRecipeStatus: draft recipes exist. Publish or delete them before rolling back this migration; the pre-draft schema cannot store their missing values.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Recipes");

            migrationBuilder.AlterColumn<int>(
                name: "Servings",
                table: "Recipes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PreparationTime",
                table: "Recipes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Recipes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CookingTime",
                table: "Recipes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
