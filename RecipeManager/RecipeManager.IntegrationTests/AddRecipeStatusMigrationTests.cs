using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class AddRecipeStatusMigrationTests
{
    // The last migration before this one: "Recipes" has no "Status" column and every scalar is NOT NULL.
    private const string PreviousMigration = "StructureInstructions";

    private readonly PostgresContainerFixture _postgres;

    public AddRecipeStatusMigrationTests(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    private AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.ConnectionStringFor($"MigrationDb_{Guid.NewGuid():N}"))
        .Options);

    [SkippableFact]
    public async Task AddRecipeStatus_ShouldMarkEveryExistingRecipePublished()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        await using AppDbContext context = NewContext();
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            var recipeId = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Recipes" ("Id","Title","Description","PreparationTime","CookingTime","Servings")
                VALUES ({recipeId}, 'Legacy', 'Written before R-19', 10, 20, 4);
                """);

            await migrator.MigrateAsync();

            string status = await context.Database
                .SqlQuery<string>($"""SELECT "Status" AS "Value" FROM "Recipes" WHERE "Id" = {recipeId}""")
                .SingleAsync();
            status.Should().Be("Published");
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [SkippableFact]
    public async Task AddRecipeStatus_Down_ShouldRefuseWhileADraftExists()
    {
        Skip.If(_postgres.SkipReason is not null, _postgres.SkipReason);

        await using AppDbContext context = NewContext();
        try
        {
            IMigrator migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "Recipes" ("Id","Title","Status") VALUES ({Guid.NewGuid()}, 'Half-written', 'Draft');""");

            Func<Task> act = () => migrator.MigrateAsync(PreviousMigration);

            // Without the guard, Down would succeed by writing 0 and '' into the draft's empty columns.
            var assertion = await act.Should().ThrowAsync<PostgresException>();
            assertion.Which.Message.Should().Contain("AddRecipeStatus");
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
