using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
        IMigrator migrator = context.Database.GetService<IMigrator>();

        // Stop at the old schema, where "Ingredients" is still a text[] column on "Recipes".
        await migrator.MigrateAsync("InitialCreate");

        var recipeId = Guid.NewGuid();
        // Parameterised throughout (both the write below and the reads further down) rather than splicing the
        // id into the SQL text — a test is exactly the place that pattern gets copied from.
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

        await context.Database.EnsureDeletedAsync();
    }
}
