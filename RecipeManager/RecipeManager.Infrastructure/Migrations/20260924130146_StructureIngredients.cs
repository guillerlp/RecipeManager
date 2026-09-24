using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StructureIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            // Pre-flight guard. Before this migration, no per-item length cap existed anywhere in the
            // system: RecipeValidationRules only capped the ingredient LIST at 50 entries, and the old
            // column was plain, unbounded text (SEC-09). A string over 200 characters would otherwise hit
            // the new Name limit below and abort the INSERT with a raw 22001. Since Program.cs applies
            // migrations automatically at startup and there is no rollback procedure (INFRA-03), that
            // failure has to surface as an actionable message instead of a crash loop with an opaque code.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Recipes" r, unnest(r."Ingredients") AS e WHERE length(e) > 200
                    ) THEN
                        RAISE EXCEPTION 'StructureIngredients: at least one existing ingredient exceeds the new 200-character limit on "RecipeIngredients"."Name". Shorten those entries before applying this migration; the backfill is deliberately lossless and will not truncate them.';
                    END IF;
                END $$;
                """);

            // Backfill. Lossless only because Quantity and Unit are nullable: every old free-text string
            // becomes a name-only ingredient at its original index. Deliberately NOT parsed — a wrong parse
            // is indistinguishable from real data afterwards (spec 010 §6).
            migrationBuilder.Sql("""
                INSERT INTO "RecipeIngredients" ("Id", "RecipeId", "Position", "Name", "Quantity", "Unit", "Notes")
                SELECT gen_random_uuid(), r."Id", t.ord - 1, t.elem, NULL, NULL, NULL
                FROM "Recipes" r, unnest(r."Ingredients") WITH ORDINALITY AS t(elem, ord);
                """);

            migrationBuilder.DropColumn(
                name: "Ingredients",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Ingredients",
                table: "Recipes",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            // LOSSY: quantities, units, and notes are discarded. Only the names survive.
            migrationBuilder.Sql("""
                UPDATE "Recipes" r
                SET "Ingredients" = COALESCE((
                    SELECT array_agg(i."Name" ORDER BY i."Position")
                    FROM "RecipeIngredients" i
                    WHERE i."RecipeId" = r."Id"
                ), ARRAY[]::text[]);
                """);

            migrationBuilder.DropTable(
                name: "RecipeIngredients");
        }
    }
}
