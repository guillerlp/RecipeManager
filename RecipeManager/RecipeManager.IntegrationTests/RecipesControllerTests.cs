using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.IntegrationTests;

public class RecipesControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateRecipe_WithValidData_ShouldReturnCreatedStatusAndSaveToDatabase()
    {
        // ==================== ARRANGE ====================
        var command = new CreateRecipeCommand(
            Title: "Integration Test Chocolate Cake",
            Description: "A delicious chocolate cake for testing",
            PreparationTime: 20,
            CookingTime: 30,
            Servings: 8,
            Ingredients: new List<string> { "Flour", "Sugar", "Cocoa powder", "Eggs" },
            Instructions: new List<string>
                { "Mix dry ingredients", "Add wet ingredients", "Bake at 350°F for 30 minutes" }
        );

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", command);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RecipeDto? createdRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>();

        createdRecipe.Should().NotBeNull();
        createdRecipe!.Id.Should().NotBeEmpty();
        createdRecipe.Title.Should().Be(command.Title);
        createdRecipe.Description.Should().Be(command.Description);
        createdRecipe.PreparationTime.Should().Be(command.PreparationTime);
        createdRecipe.CookingTime.Should().Be(command.CookingTime);
        createdRecipe.Servings.Should().Be(command.Servings);
        createdRecipe.Ingredients.Should().BeEquivalentTo(command.Ingredients);
        createdRecipe.Instructions.Should().BeEquivalentTo(command.Instructions);

        Recipe? recipeInDb = await DbContext.Recipes.FindAsync(createdRecipe.Id);
        recipeInDb.Should().NotBeNull();
        recipeInDb!.Title.Should().Be(command.Title);
    }

    [Fact]
    public async Task CreateRecipe_WithInvalidData_ShouldReturnBadRequest()
    {
        // ==================== ARRANGE ====================
        var invalidCommand = new CreateRecipeCommand(
            Title: "",
            Description: "Test description",
            PreparationTime: 10,
            CookingTime: 20,
            Servings: 4,
            Ingredients: new List<string> { "Ingredient 1" },
            Instructions: new List<string> { "Step 1" }
        );

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", invalidCommand);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var recipesInDb = DbContext.Recipes.ToList();
        recipesInDb.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecipeById_WhenRecipeExists_ShouldReturnOkWithRecipe()
    {
        // ==================== ARRANGE ====================
        var existingRecipeResult = Recipe.Create(
            "Seeded Recipe",
            "This recipe was seeded for testing",
            15,
            25,
            6,
            new List<string> { "Ingredient A", "Ingredient B" },
            new List<string> { "Step 1", "Step 2" }
        );
        Recipe existingRecipe = existingRecipeResult.Value;

        await SeedDatabase(existingRecipe);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.GetAsync($"/api/recipes/{existingRecipe.Id}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        RecipeDto? retrievedRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
        retrievedRecipe.Should().NotBeNull();
        retrievedRecipe!.Id.Should().Be(existingRecipe.Id);
        retrievedRecipe.Title.Should().Be(existingRecipe.Title);
        retrievedRecipe.Description.Should().Be(existingRecipe.Description);
    }

    [Fact]
    public async Task GetRecipeById_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // ==================== ARRANGE ====================
        var nonExistentId = Guid.NewGuid();

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.GetAsync($"/api/recipes/{nonExistentId}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllRecipes_WhenRecipesExist_ShouldReturnOkWithAllRecipes()
    {
        // ==================== ARRANGE ====================
        var recipe1Result = Recipe.Create(
            "title1",
            "desc1",
            10,
            15,
            4,
            new List<string> { "Ingredient A", "Ingredient B" },
            new List<string> { "Step 1", "Step 2" });

        var recipe2Result = Recipe.Create(
            "title2",
            "desc2",
            10,
            15,
            4,
            new List<string> { "Ingredient A", "Ingredient B" },
            new List<string> { "Step 1", "Step 2" });

        var recipe3Result = Recipe.Create(
            "title3",
            "desc3",
            10,
            15,
            4,
            new List<string> { "Ingredient A", "Ingredient B" },
            new List<string> { "Step 1", "Step 2" });

        await SeedDatabase(recipe1Result.Value, recipe2Result.Value, recipe3Result.Value);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.GetAsync("/api/recipes");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<RecipeDto>? retrievedRecipes = await response.Content.ReadFromJsonAsync<List<RecipeDto>>();
        retrievedRecipes.Should().NotBeNull();
        retrievedRecipes.Should().HaveCount(3);
        retrievedRecipes.Should().Contain(r => r.Title == "title1");
        retrievedRecipes.Should().Contain(r => r.Title == "title2");
        retrievedRecipes.Should().Contain(r => r.Title == "title3");
    }

    [Fact]
    public async Task UpdateRecipe_WithValidData_ShouldReturnOkAndUpdateDatabase()
    {
        // ==================== ARRANGE ====================
        var currentRecipeResult = Recipe.Create(
            "titleCurrent",
            "descCurrent",
            10,
            15,
            4,
            new List<string> { "Ingredient A", "Ingredient B" },
            new List<string> { "Step 1", "Step 2" });

        await SeedDatabase(currentRecipeResult.Value);

        var updateRecipeDto = new UpdateRecipeDto(
            "titleUpdate",
            "descUpdate",
            20,
            20,
            6,
            new List<string> { "Ingredient A1", "Ingredient B1" },
            new List<string> { "Step 1B", "Step 2B" });

        var currentId = currentRecipeResult.Value.Id;

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/recipes/{currentId}", updateRecipeDto);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Refresh DbContext to get latest data from database (not cached entity)
        DbContext.ChangeTracker.Clear();

        var updatedRecipe = await DbContext.Recipes.FindAsync(currentId);
        updatedRecipe.Should().NotBeNull();
        updatedRecipe.Id.Should().Be(currentId);
        updatedRecipe.Title.Should().Be(updateRecipeDto.Title);
        updatedRecipe.Description.Should().Be(updateRecipeDto.Description);
        updatedRecipe.PreparationTime.Should().Be(updateRecipeDto.PreparationTime);
        updatedRecipe.CookingTime.Should().Be(updateRecipeDto.CookingTime);
        updatedRecipe.Servings.Should().Be(updateRecipeDto.Servings);
        updatedRecipe.Ingredients.Should().BeEquivalentTo(updateRecipeDto.Ingredients);
        updatedRecipe.Instructions.Should().BeEquivalentTo(updateRecipeDto.Instructions);
    }

    [Fact]
    public async Task DeleteRecipe_WhenRecipeExists_ShouldReturnNoContentAndRemoveFromDatabase()
    {
        // ==================== ARRANGE ====================
        var existingRecipe = Recipe.Create(
            "title",
            "desc",
            10,
            15,
            4,
            new List<string> { "Ingredient A", "Ingredient B" },
            new List<string> { "Step 1", "Step 2" });

        await SeedDatabase(existingRecipe.Value);

        var existingId = existingRecipe.Value.Id;

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{existingId}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Refresh DbContext to get latest data from database (not cached entity)
        DbContext.ChangeTracker.Clear();

        var deletedRecipe = await DbContext.Recipes.FindAsync(existingId);
        deletedRecipe.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRecipe_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // ==================== ARRANGE ====================
        var randomId = Guid.NewGuid();

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{randomId}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}