using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.IntegrationTests;

/// <summary>
/// The two persistence facts ADR-028 rests on: a recipe's cooks die with it by referential action, and the stats
/// query aggregates per recipe. Both need a real PostgreSQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public class CookPersistenceTests : IntegrationTestBase
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    public CookPersistenceTests(PostgresContainerFixture postgres) : base(postgres)
    {
    }

    private ICookRepository Cooks => Scope.ServiceProvider.GetRequiredService<ICookRepository>();

    private static Recipe Draft(string title) =>
        Recipe.Create(title, null, null, null, null, [], [], RecipeStatus.Draft).Value;

    [SkippableFact]
    public async Task DeletingARecipe_ShouldDeleteItsCooks_ByCascade()
    {
        Recipe recipe = Draft("Cacio e Pepe");
        await SeedDatabase(recipe);
        await SeedDatabase(Cook.Create(recipe.Id, Now.AddDays(-1), Now).Value);

        await DbContext.Recipes.Where(r => r.Id == recipe.Id).ExecuteDeleteAsync();

        (await DbContext.Cooks.CountAsync()).Should().Be(0);
    }

    [SkippableFact]
    public async Task GetStatsAsync_ShouldCountAndTakeTheLatestPerRecipe_AndOmitUncookedRecipes()
    {
        Recipe cooked = Draft("Cooked");
        Recipe uncooked = Draft("Never cooked");
        await SeedDatabase(cooked, uncooked);
        DateTimeOffset latest = Now.AddDays(-1);
        await SeedDatabase(
            Cook.Create(cooked.Id, Now.AddDays(-9), Now).Value,
            Cook.Create(cooked.Id, latest, Now).Value,
            Cook.Create(cooked.Id, Now.AddDays(-3), Now).Value);

        IReadOnlyDictionary<Guid, CookStats> stats =
            await Cooks.GetStatsAsync([cooked.Id, uncooked.Id], CancellationToken.None);

        stats.Should().ContainKey(cooked.Id).And.NotContainKey(uncooked.Id);
        stats[cooked.Id].Count.Should().Be(3);
        // timestamptz keeps microseconds; DateTimeOffset has 100 ns ticks.
        stats[cooked.Id].LastCookedAt.Should().BeCloseTo(latest, TimeSpan.FromMilliseconds(1));
    }

    [SkippableFact]
    public async Task GetByIdAsync_ShouldNotFindAnotherRecipesCook()
    {
        Recipe a = Draft("A");
        Recipe b = Draft("B");
        await SeedDatabase(a, b);
        Cook cookOfA = Cook.Create(a.Id, null, Now).Value;
        await SeedDatabase(cookOfA);

        (await Cooks.GetByIdAsync(b.Id, cookOfA.Id, CancellationToken.None)).Should().BeNull();
        (await Cooks.GetByIdAsync(a.Id, cookOfA.Id, CancellationToken.None)).Should().NotBeNull();
    }
}
