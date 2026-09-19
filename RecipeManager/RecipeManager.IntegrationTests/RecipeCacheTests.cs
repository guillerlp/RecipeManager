using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.IntegrationTests;

/// <summary>
/// Pins the behaviour of <c>CachedRecipeRepository</c> as a client observes it (TEST-02, spec 006). Every
/// invalidation test reads through the API <b>before</b> writing: a cold cache cannot go stale, so without that
/// priming read the test would still pass with the invalidation deleted.
/// </summary>
[Collection(PostgresCollection.Name)]
public class RecipeCacheTests : IntegrationTestBase
{
    public RecipeCacheTests(PostgresContainerFixture postgres) : base(postgres)
    {
    }

    [SkippableFact]
    public async Task GetAllRecipes_WhenCalledTwice_ShouldServeSecondResponseFromCache()
    {
        // ==================== ARRANGE ====================
        Recipe recipe = CreateRecipe("Original title");
        await SeedDatabase(recipe);

        await GetAllRecipes();

        // Behind the API's back, so only a cache hit can still return the original title.
        await DbContext.Recipes
            .Where(r => r.Id == recipe.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Title, "Changed behind the API"));

        // ==================== ACT ====================
        List<RecipeDto> recipes = await GetAllRecipes();

        // ==================== ASSERT ====================
        // Deliberately asserts stale data. Proving the cache is on is what gives the invalidation tests below their
        // meaning: with caching disabled they would all pass, because nothing could be stale.
        recipes.Should().ContainSingle().Which.Title.Should().Be("Original title");
    }

    [SkippableFact]
    public async Task CreateRecipe_AfterListWasCached_ShouldIncludeNewRecipeInList()
    {
        // ==================== ARRANGE ====================
        await SeedDatabase(CreateRecipe("Existing recipe"));

        await GetAllRecipes();

        var command = new CreateRecipeCommand(
            Title: "New recipe",
            Description: "Created after the list was cached",
            PreparationTime: 10,
            CookingTime: 20,
            Servings: 2,
            Ingredients: ["Ingredient A"],
            Instructions: ["Step 1"]);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", command);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        RecipeDto? created = await response.Content.ReadFromJsonAsync<RecipeDto>();
        created.Should().NotBeNull();

        List<RecipeDto> recipes = await GetAllRecipes();
        recipes.Should().HaveCount(2);
        recipes.Should().Contain(r => r.Id == created.Id);
    }

    [SkippableFact]
    public async Task UpdateRecipe_AfterListAndDetailWereCached_ShouldReturnNewValuesFromBoth()
    {
        // ==================== ARRANGE ====================
        Recipe recipe = CreateRecipe("Original title");
        await SeedDatabase(recipe);

        await GetAllRecipes();
        (await Client.GetAsync($"/api/recipes/{recipe.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var update = new UpdateRecipeDto(
            "Updated title",
            "Updated description",
            5,
            25,
            3,
            ["Ingredient B"],
            ["Step 1B"]);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/recipes/{recipe.Id}", update);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        List<RecipeDto> recipes = await GetAllRecipes();
        recipes.Should().ContainSingle().Which.Title.Should().Be("Updated title");

        // The user-visible contract, but not on its own a test of recipe_{id} invalidation: the handler mutates
        // the instance the cache holds, so that entry already carries the new values (BUG-14).
        RecipeDto? detail = await Client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{recipe.Id}");
        detail.Should().NotBeNull();
        detail.Title.Should().Be("Updated title");
        detail.Description.Should().Be("Updated description");
    }

    [SkippableFact]
    public async Task DeleteRecipe_AfterListAndDetailWereCached_ShouldReturnNotFoundAndExcludeFromList()
    {
        // ==================== ARRANGE ====================
        Recipe recipe = CreateRecipe("To be deleted");
        await SeedDatabase(recipe);

        await GetAllRecipes();
        (await Client.GetAsync($"/api/recipes/{recipe.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{recipe.Id}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Client.GetAsync($"/api/recipes/{recipe.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        List<RecipeDto> recipes = await GetAllRecipes();
        recipes.Should().BeEmpty();
    }

    private static Recipe CreateRecipe(string title) =>
        Recipe.Create(title, "Description", 10, 15, 4, ["Ingredient A"], ["Step 1"]).Value;

    private async Task<List<RecipeDto>> GetAllRecipes()
    {
        HttpResponseMessage response = await Client.GetAsync("/api/recipes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<RecipeDto>? recipes = await response.Content.ReadFromJsonAsync<List<RecipeDto>>();
        recipes.Should().NotBeNull();
        return recipes;
    }
}
