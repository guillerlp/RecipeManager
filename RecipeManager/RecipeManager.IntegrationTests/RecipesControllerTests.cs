using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class RecipesControllerTests : IntegrationTestBase
{
    public RecipesControllerTests(PostgresContainerFixture postgres) : base(postgres)
    {
    }

    private static Ingredient Ing(string name) => Ingredient.Create(null, null, null, name, null).Value;

    private static InstructionStep Step(string text) => InstructionStep.Create(text, null, []).Value;

    private static InstructionStepInputDto StepInput(string text, params int[] ingredientIndexes) =>
        new(text, null, [.. ingredientIndexes]);

    [SkippableFact]
    public async Task CreateRecipe_WithValidData_ShouldReturnCreatedStatusAndSaveToDatabase()
    {
        // ==================== ARRANGE ====================
        var command = new CreateRecipeCommand(
            Title: "Integration Test Chocolate Cake",
            Description: "A delicious chocolate cake for testing",
            PreparationTime: 20,
            CookingTime: 30,
            Servings: 8,
            Ingredients:
            [
                new IngredientInputDto(null, null, null, "Flour", null),
                new IngredientInputDto(null, null, null, "Sugar", null),
                new IngredientInputDto(null, null, null, "Cocoa powder", null),
                new IngredientInputDto(null, null, null, "Eggs", null)
            ],
            Instructions: [StepInput("Mix dry ingredients"), StepInput("Add wet ingredients"), StepInput("Bake at 350°F for 30 minutes")]
        );

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", command, JsonOptions);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RecipeDto? createdRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions);

        createdRecipe.Should().NotBeNull();
        createdRecipe.Id.Should().NotBeEmpty();
        createdRecipe.Title.Should().Be(command.Title);
        createdRecipe.Description.Should().Be(command.Description);
        createdRecipe.PreparationTime.Should().Be(command.PreparationTime);
        createdRecipe.CookingTime.Should().Be(command.CookingTime);
        createdRecipe.Servings.Should().Be(command.Servings);
        createdRecipe.Ingredients.Select(i => i.Name).Should().Equal("Flour", "Sugar", "Cocoa powder", "Eggs");
        createdRecipe.Instructions.Select(s => s.Text).Should().Equal(command.Instructions.Select(s => s.Text));

        Recipe? recipeInDb = await DbContext.Recipes.FindAsync(createdRecipe.Id);
        recipeInDb.Should().NotBeNull();
        recipeInDb.Title.Should().Be(command.Title);
    }

    [SkippableFact]
    public async Task CreateRecipe_WithInvalidData_ShouldReturnBadRequest()
    {
        // ==================== ARRANGE ====================
        var invalidCommand = new CreateRecipeCommand(
            Title: "",
            Description: "Test description",
            PreparationTime: 10,
            CookingTime: 20,
            Servings: 4,
            Ingredients: [new IngredientInputDto(null, null, null, "Ingredient 1", null)],
            Instructions: [StepInput("Step 1")]
        );

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", invalidCommand, JsonOptions);

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var recipesInDb = DbContext.Recipes.ToList();
        recipesInDb.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task GetRecipeById_WhenRecipeExists_ShouldReturnOkWithRecipe()
    {
        // ==================== ARRANGE ====================
        var existingRecipeResult = Recipe.Create(
            "Seeded Recipe",
            "This recipe was seeded for testing",
            15,
            25,
            6,
            new List<Ingredient> { Ing("Ingredient A"), Ing("Ingredient B") },
            new List<InstructionStep> { Step("Step 1"), Step("Step 2") }
        );
        Recipe existingRecipe = existingRecipeResult.Value;

        await SeedDatabase(existingRecipe);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.GetAsync($"/api/recipes/{existingRecipe.Id}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        RecipeDto? retrievedRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions);
        retrievedRecipe.Should().NotBeNull();
        retrievedRecipe.Id.Should().Be(existingRecipe.Id);
        retrievedRecipe.Title.Should().Be(existingRecipe.Title);
        retrievedRecipe.Description.Should().Be(existingRecipe.Description);
    }

    [SkippableFact]
    public async Task GetRecipeById_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // ==================== ARRANGE ====================
        var nonExistentId = Guid.NewGuid();

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.GetAsync($"/api/recipes/{nonExistentId}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetAllRecipes_WhenRecipesExist_ShouldReturnOkWithAllRecipes()
    {
        // ==================== ARRANGE ====================
        var recipe1Result = Recipe.Create(
            "title1",
            "desc1",
            10,
            15,
            4,
            new List<Ingredient> { Ing("Ingredient A"), Ing("Ingredient B") },
            new List<InstructionStep> { Step("Step 1"), Step("Step 2") });

        var recipe2Result = Recipe.Create(
            "title2",
            "desc2",
            10,
            15,
            4,
            new List<Ingredient> { Ing("Ingredient A"), Ing("Ingredient B") },
            new List<InstructionStep> { Step("Step 1"), Step("Step 2") });

        var recipe3Result = Recipe.Create(
            "title3",
            "desc3",
            10,
            15,
            4,
            new List<Ingredient> { Ing("Ingredient A"), Ing("Ingredient B") },
            new List<InstructionStep> { Step("Step 1"), Step("Step 2") });

        await SeedDatabase(recipe1Result.Value, recipe2Result.Value, recipe3Result.Value);

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.GetAsync("/api/recipes");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<RecipeDto>? retrievedRecipes = await response.Content.ReadFromJsonAsync<List<RecipeDto>>(JsonOptions);
        retrievedRecipes.Should().NotBeNull();
        retrievedRecipes.Should().HaveCount(3);
        retrievedRecipes.Should().Contain(r => r.Title == "title1");
        retrievedRecipes.Should().Contain(r => r.Title == "title2");
        retrievedRecipes.Should().Contain(r => r.Title == "title3");
    }

    [SkippableFact]
    public async Task UpdateRecipe_WithValidData_ShouldReturnOkAndUpdateDatabase()
    {
        // ==================== ARRANGE ====================
        var currentRecipeResult = Recipe.Create(
            "titleCurrent",
            "descCurrent",
            10,
            15,
            4,
            new List<Ingredient> { Ing("Ingredient A"), Ing("Ingredient B") },
            new List<InstructionStep> { Step("Step 1"), Step("Step 2") });

        await SeedDatabase(currentRecipeResult.Value);

        var updateRecipeDto = new UpdateRecipeDto(
            "titleUpdate",
            "descUpdate",
            20,
            20,
            6,
            [
                new IngredientInputDto(null, null, null, "Ingredient A1", null),
                new IngredientInputDto(null, null, null, "Ingredient B1", null)
            ],
            [StepInput("Step 1B"), StepInput("Step 2B")]);

        var currentId = currentRecipeResult.Value.Id;

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/recipes/{currentId}", updateRecipeDto, JsonOptions);

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
        updatedRecipe.Ingredients.Select(i => i.Name).Should().Equal("Ingredient A1", "Ingredient B1");
        updatedRecipe.Instructions.Select(s => s.Text).Should().Equal(updateRecipeDto.Instructions.Select(s => s.Text));
    }

    [SkippableFact]
    public async Task DeleteRecipe_WhenRecipeExists_ShouldReturnNoContentAndRemoveFromDatabase()
    {
        // ==================== ARRANGE ====================
        var existingRecipe = Recipe.Create(
            "title",
            "desc",
            10,
            15,
            4,
            new List<Ingredient> { Ing("Ingredient A"), Ing("Ingredient B") },
            new List<InstructionStep> { Step("Step 1"), Step("Step 2") });

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

    [SkippableFact]
    public async Task DeleteRecipe_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // ==================== ARRANGE ====================
        var randomId = Guid.NewGuid();

        // ==================== ACT ====================
        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{randomId}");

        // ==================== ASSERT ====================
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task CreateRecipe_WithStructuredIngredients_ShouldRoundTripEveryField()
    {
        var command = new CreateRecipeCommand(
            Title: "Butter Sauce",
            Description: "Two ingredients, three shapes",
            PreparationTime: 5,
            CookingTime: 5,
            Servings: 2,
            Ingredients:
            [
                new IngredientInputDto(null, 2m, Unit.Tablespoon, "Butter", "cold"),
                new IngredientInputDto(null, 3m, null, "Eggs", null),
                new IngredientInputDto(null, null, null, "Salt to taste", null),
            ],
            Instructions: [StepInput("Melt"), StepInput("Whisk")]
        );

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", command, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RecipeDto? created = await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions);
        created.Should().NotBeNull();

        created.Ingredients.Select(i => i.Name).Should().Equal("Butter", "Eggs", "Salt to taste");
        created.Ingredients.Should().AllSatisfy(i => i.Id.Should().NotBeEmpty());

        IngredientDto butter = created.Ingredients[0];
        butter.Quantity.Should().Be(2m);
        butter.Unit.Should().Be(Unit.Tablespoon);
        butter.Notes.Should().Be("cold");

        created.Ingredients[2].Quantity.Should().BeNull();
        created.Ingredients[2].Unit.Should().BeNull();

        // The assertions above only prove the POST response was serialized correctly; re-read from the
        // database to prove the same values were actually persisted, not just echoed back.
        RecipeDto? reread = await Client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{created.Id}", JsonOptions);
        reread.Should().NotBeNull();

        reread.Ingredients.Select(i => i.Name).Should().Equal("Butter", "Eggs", "Salt to taste");

        IngredientDto rereadButter = reread.Ingredients[0];
        rereadButter.Id.Should().Be(butter.Id);
        rereadButter.Quantity.Should().Be(2m);
        rereadButter.Unit.Should().Be(Unit.Tablespoon);
        rereadButter.Notes.Should().Be("cold");

        reread.Ingredients[2].Quantity.Should().BeNull();
        reread.Ingredients[2].Unit.Should().BeNull();
    }

    [SkippableFact]
    public async Task UpdateRecipe_WhenReordered_ShouldKeepTheIdsAndTheNewOrder()
    {
        // The whole reason Ingredient is an entity: R-17's step references must survive an edit.
        var create = new CreateRecipeCommand("Reorder me", "Description", 5, 5, 2,
            [
                new IngredientInputDto(null, 1m, Unit.Cup, "A", null),
                new IngredientInputDto(null, 2m, Unit.Cup, "B", null),
                new IngredientInputDto(null, 3m, Unit.Cup, "C", null),
            ],
            [StepInput("Step")]);

        HttpResponseMessage createResponse = await Client.PostAsJsonAsync("/api/recipes", create, JsonOptions);
        RecipeDto created = (await createResponse.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions))!;

        IngredientDto a = created.Ingredients.Single(i => i.Name == "A");
        IngredientDto b = created.Ingredients.Single(i => i.Name == "B");
        IngredientDto c = created.Ingredients.Single(i => i.Name == "C");

        var update = new UpdateRecipeDto("Reorder me", "Description", 5, 5, 2,
            [
                new IngredientInputDto(c.Id, c.Quantity, c.Unit, c.Name, c.Notes),
                new IngredientInputDto(a.Id, a.Quantity, a.Unit, a.Name, a.Notes),
                new IngredientInputDto(b.Id, b.Quantity, b.Unit, b.Name, b.Notes),
            ],
            [StepInput("Step")]);

        HttpResponseMessage updateResponse = await Client.PutAsJsonAsync($"/api/recipes/{created.Id}", update, JsonOptions);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        RecipeDto? reread = await Client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{created.Id}", JsonOptions);

        reread.Should().NotBeNull();
        reread.Ingredients.Select(i => i.Name).Should().Equal("C", "A", "B");
        reread.Ingredients.Select(i => i.Id).Should().Equal(c.Id, a.Id, b.Id);
    }

    [SkippableFact]
    public async Task UpdateRecipe_WithANullId_ShouldMintANewIdAndKeepTheOthers()
    {
        var create = new CreateRecipeCommand("Add one", "Description", 5, 5, 2,
            [new IngredientInputDto(null, 1m, Unit.Cup, "Existing", null)], [StepInput("Step")]);

        RecipeDto created = (await (await Client.PostAsJsonAsync("/api/recipes", create, JsonOptions))
            .Content.ReadFromJsonAsync<RecipeDto>(JsonOptions))!;
        Guid existingId = created.Ingredients.Single().Id;

        var update = new UpdateRecipeDto("Add one", "Description", 5, 5, 2,
            [
                new IngredientInputDto(existingId, 1m, Unit.Cup, "Existing", null),
                new IngredientInputDto(null, 2m, Unit.Cup, "Brand new", null),
            ],
            [StepInput("Step")]);

        await Client.PutAsJsonAsync($"/api/recipes/{created.Id}", update, JsonOptions);

        RecipeDto reread = (await Client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{created.Id}", JsonOptions))!;

        reread.Ingredients.Should().HaveCount(2);
        reread.Ingredients[0].Id.Should().Be(existingId);
        reread.Ingredients[1].Id.Should().NotBeEmpty().And.NotBe(existingId);
    }

    [SkippableFact]
    public async Task DeleteRecipe_ThroughTheApi_ShouldLeaveNoOrphanIngredientRowsForThatRecipe()
    {
        // This proves EF's in-memory cascade (owned collections are always loaded, so EF deletes their
        // rows itself), not the database's: it would still pass even with no ON DELETE CASCADE at all.
        // The database-level guarantee is exercised separately below, bypassing EF entirely.
        var create = new CreateRecipeCommand("Delete me", "Description", 5, 5, 2,
            [new IngredientInputDto(null, 1m, Unit.Cup, "Doomed", null)], [StepInput("Step")]);

        RecipeDto created = (await (await Client.PostAsJsonAsync("/api/recipes", create, JsonOptions))
            .Content.ReadFromJsonAsync<RecipeDto>(JsonOptions))!;

        HttpResponseMessage response = await Client.DeleteAsync($"/api/recipes/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        DbContext.ChangeTracker.Clear();

        int orphans = await DbContext.Database
            .SqlQuery<int>(
                $"SELECT COUNT(*)::int AS \"Value\" FROM \"RecipeIngredients\" WHERE \"RecipeId\" = {created.Id}")
            .SingleAsync();

        orphans.Should().Be(0);
    }

    [SkippableFact]
    public async Task DeleteRecipe_ViaRawSql_ShouldCascadeAtTheDatabaseLevel()
    {
        // Deletes the Recipes row directly, bypassing EF (and its in-memory owned-collection cascade)
        // entirely, so this is the test that actually exercises "RecipeIngredients"."RecipeId" ON DELETE
        // CASCADE rather than relying on application code to clean up.
        var create = new CreateRecipeCommand("Delete me via SQL", "Description", 5, 5, 2,
            [new IngredientInputDto(null, 1m, Unit.Cup, "Doomed", null)], [StepInput("Step")]);

        RecipeDto created = (await (await Client.PostAsJsonAsync("/api/recipes", create, JsonOptions))
            .Content.ReadFromJsonAsync<RecipeDto>(JsonOptions))!;

        await DbContext.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "Recipes" WHERE "Id" = {created.Id}
            """);

        DbContext.ChangeTracker.Clear();

        int orphans = await DbContext.Database
            .SqlQuery<int>(
                $"SELECT COUNT(*)::int AS \"Value\" FROM \"RecipeIngredients\" WHERE \"RecipeId\" = {created.Id}")
            .SingleAsync();

        orphans.Should().Be(0);
    }

    [SkippableTheory]
    // Note all three are 422, not 400. Quantity 0 passes FluentValidation on purpose —
    // InclusiveBetween(0, 100000) guards the *bound*, and "must be positive" is a business rule the
    // domain owns. Same split as Title = "" (422) vs. Title = null (400).
    [InlineData(null, "Gram", "Flour")]      // unit without quantity
    [InlineData(0d, "Gram", "Flour")]        // non-positive quantity
    [InlineData(1d, "Gram", "   ")]          // blank name
    public async Task CreateRecipe_WithAnInvalidIngredient_ShouldReturn422(double? quantity, string unit,
        string name)
    {
        var command = new CreateRecipeCommand("Bad ingredient", "Description", 5, 5, 2,
            [new IngredientInputDto(null, (decimal?)quantity, Enum.Parse<Unit>(unit), name, null)], [StepInput("Step")]);

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", command, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().Contain("ingredients");
    }

    [SkippableFact]
    public async Task CreateRecipe_WithAnOverlongIngredientName_ShouldReturn400()
    {
        // SEC-09, ingredient half: the cap is in FluentValidation *and* in the database.
        var command = new CreateRecipeCommand("Long name", "Description", 5, 5, 2,
            [new IngredientInputDto(null, 1m, Unit.Gram, new string('x', 201), null)], [StepInput("Step")]);

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/recipes", command, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task CreateRecipe_WithAnUnrecognisedUnit_ShouldReturn400()
    {
        // The enum is the allow-list. An unknown unit never reaches a handler — model binding rejects it.
        // Posted as raw JSON because IngredientInputDto cannot express an invalid Unit.
        var json = new StringContent("""
            {
              "title": "Bad unit", "description": "Description",
              "preparationTime": 5, "cookingTime": 5, "servings": 2,
              "ingredients": [{ "id": null, "quantity": 1, "unit": "Furlong", "name": "Flour", "notes": null }],
              "instructions": [{ "text": "Step", "durationMinutes": null, "ingredientIndexes": [] }]
            }
            """, System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await Client.PostAsync("/api/recipes", json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // Without this, an unrelated binding failure earlier in the payload would also produce a bare
        // 400 and pass here for the wrong reason.
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("unit");
    }
}
