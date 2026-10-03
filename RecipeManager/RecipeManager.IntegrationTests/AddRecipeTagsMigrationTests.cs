using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RecipeManager.Domain.Entities;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class AddRecipeTagsMigrationTests
{
    // The last migration before this one: "Recipes" has no "Tags" column.
    private const string PreviousMigration = "AddRecipeStatus";

    private readonly PostgresContainerFixture _postgres;

    public AddRecipeTagsMigrationTests(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    private AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.ConnectionStringFor($"MigrationDb_{Guid.NewGuid():N}"))
        .Options);

    [SkippableFact]
    public async Task AddRecipeTags_ShouldGiveEveryExistingRecipeAnEmptyTagList()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        await using AppDbContext context = NewContext();
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            var recipeId = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "Recipes" ("Id","Title","Status") VALUES ({recipeId}, 'Legacy', 'Draft');""");

            await migrator.MigrateAsync();

            // Read back through the model, as the API would: a NOT NULL column with no back-fill would have
            // failed the migration, and a NULL slipping through would fail materialisation here.
            Recipe legacy = await context.Recipes.AsNoTracking().SingleAsync(r => r.Id == recipeId);
            legacy.Tags.Should().BeEmpty();
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
