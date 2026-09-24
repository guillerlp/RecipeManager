using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class StructureIngredientsMigrationTests
{
    private readonly PostgresContainerFixture _postgres;

    public StructureIngredientsMigrationTests(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task StructureIngredients_ShouldBackfillEveryFreeTextStringAtItsOriginalIndex()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        string connectionString = _postgres.ConnectionStringFor($"MigrationDb_{Guid.NewGuid():N}");

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();

            // Stop at the old schema, where "Ingredients" is still a text[] column on "Recipes".
            await migrator.MigrateAsync("InitialCreate");

            var recipeId = Guid.NewGuid();
            // Parameterised throughout (both the write below and the reads further down) rather than splicing
            // the id into the SQL text — a test is exactly the place that pattern gets copied from.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Recipes"
                    ("Id","Title","Description","PreparationTime","CookingTime","Servings","Ingredients","Instructions")
                VALUES
                    ({recipeId}, 'Legacy', 'Written before R-10', 10, 20, 4,
                     ARRAY['flour','water','200g butter'], ARRAY['Mix','Bake']);
                """);

            await migrator.MigrateAsync();

            var rows = await context.Database
                .SqlQuery<string>($"""
                    SELECT i."Name" AS "Value"
                    FROM "RecipeIngredients" i
                    WHERE i."RecipeId" = {recipeId}
                    ORDER BY i."Position"
                    """)
                .ToListAsync();

            // Order preserved, nothing parsed: "200g butter" stays one name, exactly as ADR-022 decided.
            rows.Should().Equal("flour", "water", "200g butter");

            var quantities = await context.Database
                .SqlQuery<decimal?>($"""
                    SELECT i."Quantity" AS "Value"
                    FROM "RecipeIngredients" i
                    WHERE i."RecipeId" = {recipeId}
                    """)
                .ToListAsync();

            quantities.Should().AllSatisfy(q => q.Should().BeNull());
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [SkippableFact]
    public async Task StructureIngredients_ShouldRejectAPreExistingIngredientOver200Characters()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        string connectionString = _postgres.ConnectionStringFor($"MigrationDb_{Guid.NewGuid():N}");

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();

            // Stop at the old schema, where "Ingredients" is still an unbounded text[] column on "Recipes"
            // (SEC-09) — nothing before this migration caps an ingredient string's length.
            await migrator.MigrateAsync("InitialCreate");

            var recipeId = Guid.NewGuid();
            string tooLong = new string('a', 201);
            // Parameterised, per the idiom above: the 201-character string never gets spliced into the SQL text.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Recipes"
                    ("Id","Title","Description","PreparationTime","CookingTime","Servings","Ingredients","Instructions")
                VALUES
                    ({recipeId}, 'Legacy', 'Written before R-10', 10, 20, 4,
                     ARRAY[{tooLong}], ARRAY['Mix','Bake']);
                """);

            Func<Task> act = () => migrator.MigrateAsync();

            // Assert on the guard's own message, not merely that migrating threw: a bare PostgreSQL 22001
            // from the later NOT NULL/length-capped column would also throw here if the guard were ever
            // deleted or broken, and that failure must not read as this test passing.
            var assertion = await act.Should().ThrowAsync<PostgresException>();
            assertion.Which.Message.Should().Contain("StructureIngredients");
            assertion.Which.Message.Should().Contain("200-character");
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
