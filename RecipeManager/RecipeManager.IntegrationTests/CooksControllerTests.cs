using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RecipeManager.Application.DTO.Cooks;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class CooksControllerTests : IntegrationTestBase
{
    public CooksControllerTests(PostgresContainerFixture postgres) : base(postgres)
    {
    }

    private static Recipe Draft(string title = "Cacio e Pepe") =>
        Recipe.Create(title, null, null, null, null, [], [], RecipeStatus.Draft).Value;

    private Task<HttpResponseMessage> PostCook(Guid recipeId, string json) =>
        Client.PostAsync($"/api/recipes/{recipeId}/cooks", new StringContent(json, Encoding.UTF8, "application/json"));

    [SkippableFact]
    public async Task LogCook_WithAnEmptyBody_ShouldStoreACookAtNow()
    {
        Recipe recipe = Draft();
        await SeedDatabase(recipe);
        DateTimeOffset before = DateTimeOffset.UtcNow;

        HttpResponseMessage response = await PostCook(recipe.Id, "{}");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CookDto cook = (await response.Content.ReadFromJsonAsync<CookDto>(JsonOptions))!;
        cook.CookedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
        // No GET for one cook: Location identifies the resource, which answers DELETE (RFC 9110 §10.2.2).
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/recipes/{recipe.Id}/cooks/{cook.Id}");
        (await DbContext.Cooks.SingleAsync()).Id.Should().Be(cook.Id);
    }

    [SkippableFact]
    public async Task LogCook_WithAPastDateInAnotherOffset_ShouldStoreTheSameInstantInUtc()
    {
        Recipe recipe = Draft();
        await SeedDatabase(recipe);

        HttpResponseMessage response = await PostCook(recipe.Id, """{ "cookedAt": "2026-10-08T12:00:00+05:30" }""");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Cook stored = await DbContext.Cooks.AsNoTracking().SingleAsync();
        stored.CookedAt.Should().Be(new DateTimeOffset(2026, 10, 8, 6, 30, 0, TimeSpan.Zero));
    }

    [SkippableFact]
    public async Task LogCook_WithAFutureDate_ShouldReturn422OnCookedAtAndStoreNothing()
    {
        Recipe recipe = Draft();
        await SeedDatabase(recipe);
        string tomorrow = DateTimeOffset.UtcNow.AddDays(1).ToString("O");

        HttpResponseMessage response = await PostCook(recipe.Id, $$"""{ "cookedAt": "{{tomorrow}}" }""");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("cookedAt");
        (await DbContext.Cooks.CountAsync()).Should().Be(0);
    }

    [SkippableFact]
    public async Task LogCook_WithAMalformedDate_ShouldReturn400()
    {
        Recipe recipe = Draft();
        await SeedDatabase(recipe);

        (await PostCook(recipe.Id, """{ "cookedAt": "not-a-date" }""")).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task LogCook_WithNoBodyAtAll_ShouldReturn415()
    {
        // Pinned for R-23 and scripts: "just now" is `{}`, not an empty request. With no body there is no
        // Content-Type, so [FromBody] rejects it before model binding — a client error, never a 500.
        Recipe recipe = Draft();
        await SeedDatabase(recipe);

        HttpResponseMessage response = await Client.PostAsync($"/api/recipes/{recipe.Id}/cooks", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await DbContext.Cooks.CountAsync()).Should().Be(0);
    }

    [SkippableFact]
    public async Task LogCook_ForAMissingRecipe_ShouldReturn404()
    {
        (await PostCook(Guid.NewGuid(), "{}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task DeleteCook_ShouldRemoveIt()
    {
        Recipe recipe = Draft();
        await SeedDatabase(recipe);
        Cook cook = Cook.Create(recipe.Id, null, DateTimeOffset.UtcNow).Value;
        await SeedDatabase(cook);

        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{recipe.Id}/cooks/{cook.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        DbContext.ChangeTracker.Clear();
        (await DbContext.Cooks.CountAsync()).Should().Be(0);
    }

    [SkippableFact]
    public async Task DeleteCook_UnderAnotherRecipe_ShouldReturn404AndKeepIt()
    {
        Recipe a = Draft("A");
        Recipe b = Draft("B");
        await SeedDatabase(a, b);
        Cook cookOfA = Cook.Create(a.Id, null, DateTimeOffset.UtcNow).Value;
        await SeedDatabase(cookOfA);

        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{b.Id}/cooks/{cookOfA.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        DbContext.ChangeTracker.Clear();
        (await DbContext.Cooks.CountAsync()).Should().Be(1);
    }

    [SkippableFact]
    public async Task GetRecipe_ShouldCarryCountAndLatestCook_FromListAndDetail()
    {
        Recipe cooked = Draft("Cooked");
        Recipe uncooked = Draft("Never cooked");
        await SeedDatabase(cooked, uncooked);
        var latest = new DateTimeOffset(2026, 10, 8, 19, 0, 0, TimeSpan.Zero);
        await SeedDatabase(
            Cook.Create(cooked.Id, latest.AddDays(-5), DateTimeOffset.UtcNow).Value,
            Cook.Create(cooked.Id, latest, DateTimeOffset.UtcNow).Value,
            Cook.Create(cooked.Id, latest.AddDays(-2), DateTimeOffset.UtcNow).Value);

        RecipeDto detail = (await Client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{cooked.Id}", JsonOptions))!;
        List<RecipeDto> drafts =
            (await Client.GetFromJsonAsync<List<RecipeDto>>("/api/recipes?status=Draft", JsonOptions))!;

        detail.CookCount.Should().Be(3);
        detail.LastCookedAt.Should().Be(latest);
        drafts.Single(r => r.Id == cooked.Id).CookCount.Should().Be(3);
        drafts.Single(r => r.Id == uncooked.Id).CookCount.Should().Be(0);
        drafts.Single(r => r.Id == uncooked.Id).LastCookedAt.Should().BeNull();
    }

    [SkippableFact]
    public async Task DeleteRecipe_ShouldDeleteItsCooks()
    {
        Recipe recipe = Draft();
        await SeedDatabase(recipe);
        await SeedDatabase(Cook.Create(recipe.Id, null, DateTimeOffset.UtcNow).Value);

        (await Client.DeleteAsync($"/api/recipes/{recipe.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        DbContext.ChangeTracker.Clear();
        (await DbContext.Cooks.CountAsync()).Should().Be(0);
    }
}
