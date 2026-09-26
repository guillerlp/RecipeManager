using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StructureInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF scaffolded DropColumn first; it is moved to the end so the backfill below can still read the
            // old text[] column.
            migrationBuilder.CreateTable(
                name: "RecipeInstructionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IngredientIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeInstructionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeInstructionSteps_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeInstructionSteps_RecipeId",
                table: "RecipeInstructionSteps",
                column: "RecipeId");

            // Pre-flight guard. Before this migration no per-step length cap existed anywhere: the validator
            // capped only the LIST, and the column was unbounded text[] (SEC-09). A step over 2000 characters
            // would hit the new Text limit below and abort the INSERT with a raw 22001 — and because Program.cs
            // applies migrations at startup with no rollback procedure (INFRA-03), that means a crash loop with
            // an opaque code. Spec 010 §6's correction; same guard, new column.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Recipes" r, unnest(r."Instructions") AS e WHERE length(e) > 2000
                    ) THEN
                        RAISE EXCEPTION 'StructureInstructions: at least one existing instruction exceeds the new 2000-character limit on "RecipeInstructionSteps"."Text". Shorten those steps before applying this migration; the backfill is deliberately lossless and will not truncate them.';
                    END IF;
                END $$;
                """);

            // Backfill. Lossless because duration and references are optional: every old string becomes a step
            // at its original index, untimed, referencing nothing.
            migrationBuilder.Sql("""
                INSERT INTO "RecipeInstructionSteps" ("Id", "RecipeId", "Position", "Text", "DurationMinutes", "IngredientIds")
                SELECT gen_random_uuid(), r."Id", t.ord - 1, t.elem, NULL, '{}'::uuid[]
                FROM "Recipes" r, unnest(r."Instructions") WITH ORDINALITY AS t(elem, ord);
                """);

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Instructions",
                table: "Recipes",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            // LOSSY: durations and ingredient references are discarded. Only the text survives.
            migrationBuilder.Sql("""
                UPDATE "Recipes" r
                SET "Instructions" = COALESCE((
                    SELECT array_agg(s."Text" ORDER BY s."Position")
                    FROM "RecipeInstructionSteps" s
                    WHERE s."RecipeId" = r."Id"
                ), ARRAY[]::text[]);
                """);

            migrationBuilder.DropTable(
                name: "RecipeInstructionSteps");
        }
    }
}
