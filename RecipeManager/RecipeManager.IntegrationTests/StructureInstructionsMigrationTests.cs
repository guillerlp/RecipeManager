using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class StructureInstructionsMigrationTests
{
    // The last migration before this one: "Instructions" is still a text[] column on "Recipes" there.
    private const string PreviousMigration = "SyncIdValueGeneration";

    private readonly PostgresContainerFixture _postgres;

    public StructureInstructionsMigrationTests(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    private AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.ConnectionStringFor($"MigrationDb_{Guid.NewGuid():N}"))
        .Options);

    // Parameterised, like StructureIngredientsMigrationTests: the string[] binds to text[] through Npgsql, so
    // no value is ever spliced into the SQL text.
    private static Task InsertLegacyRecipe(AppDbContext context, Guid recipeId, string[] instructions) =>
        context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "Recipes" ("Id","Title","Description","PreparationTime","CookingTime","Servings","Instructions")
            VALUES ({recipeId}, 'Legacy', 'Written before R-17', 10, 20, 4, {instructions});
            """);

    [SkippableFact]
    public async Task StructureInstructions_ShouldBackfillEveryStringAtItsOriginalIndex()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        await using AppDbContext context = NewContext();
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            var recipeId = Guid.NewGuid();
            await InsertLegacyRecipe(context, recipeId, ["Mix", "Bake"]);

            await migrator.MigrateAsync();

            var texts = await context.Database
                .SqlQuery<string>($"""
                    SELECT s."Text" AS "Value" FROM "RecipeInstructionSteps" s
                    WHERE s."RecipeId" = {recipeId} ORDER BY s."Position"
                    """)
                .ToListAsync();
            texts.Should().Equal("Mix", "Bake");

            var positions = await context.Database
                .SqlQuery<int>($"""
                    SELECT s."Position" AS "Value" FROM "RecipeInstructionSteps" s
                    WHERE s."RecipeId" = {recipeId} ORDER BY s."Position"
                    """)
                .ToListAsync();
            positions.Should().Equal(0, 1);

            int withDurationOrReferences = await context.Database
                .SqlQuery<int>($"""
                    SELECT COUNT(*)::int AS "Value" FROM "RecipeInstructionSteps" s
                    WHERE s."RecipeId" = {recipeId}
                      AND (s."DurationMinutes" IS NOT NULL OR cardinality(s."IngredientIds") > 0)
                    """)
                .SingleAsync();
            withDurationOrReferences.Should().Be(0);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [SkippableFact]
    public async Task StructureInstructions_ShouldRejectAPreExistingInstructionOver2000Characters()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        await using AppDbContext context = NewContext();
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            await InsertLegacyRecipe(context, Guid.NewGuid(), [new string('a', 2001)]);

            Func<Task> act = () => migrator.MigrateAsync();

            // Assert on the guard's own message: a bare 22001 from the varchar(2000) column would also throw
            // here if the guard were ever removed, and must not read as this test passing.
            var assertion = await act.Should().ThrowAsync<PostgresException>();
            assertion.Which.Message.Should().Contain("StructureInstructions");
            assertion.Which.Message.Should().Contain("2000-character");

            // The migration runs in one transaction, so the abort must leave the old column in place.
            int oldColumn = await context.Database
                .SqlQuery<int>($"""
                    SELECT COUNT(*)::int AS "Value" FROM information_schema.columns
                    WHERE table_name = 'Recipes' AND column_name = 'Instructions'
                    """)
                .SingleAsync();
            oldColumn.Should().Be(1);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
