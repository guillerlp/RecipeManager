using FluentAssertions;
using NSubstitute;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

/// <summary>
/// Unit tests for GetAllRecipesHandler
/// </summary>
public class GetAllRecipesHandlerTests
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly GetAllRecipesHandler _handler;

    public GetAllRecipesHandlerTests()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _handler = new GetAllRecipesHandler(_recipeRepository);
    }

    private static Ingredient Ing(string name) => Ingredient.Create(null, null, null, name, null).Value;

    private static InstructionStep Step(string text) => InstructionStep.Create(text, null, []).Value;

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithMultipleRecipes_ShouldReturnAllRecipeDtos()
    {
        // Arrange
        var recipe1Result = Recipe.Create(
            "Chocolate Cake",
            "Delicious cake",
            20,
            30,
            8,
            new List<Ingredient> { Ing("Flour"), Ing("Sugar") },
            new List<InstructionStep> { Step("Mix"), Step("Bake") }
        );

        var recipe2Result = Recipe.Create(
            "Pasta",
            "Italian pasta",
            10,
            15,
            4,
            new List<Ingredient> { Ing("Pasta"), Ing("Tomatoes") },
            new List<InstructionStep> { Step("Boil"), Step("Mix") }
        );

        var recipe3Result = Recipe.Create(
            "Salad",
            "Fresh salad",
            10,
            0,
            2,
            new List<Ingredient> { Ing("Lettuce"), Ing("Tomatoes") },
            new List<InstructionStep> { Step("Chop"), Step("Mix") }
        );

        var recipes = new List<Recipe>
        {
            recipe1Result.Value,
            recipe2Result.Value,
            recipe3Result.Value
        };

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(recipes);

        var query = new GetAllRecipesQuery();

        // Act
        IEnumerable<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        result.Should().Contain(r => r.Title == "Chocolate Cake");
        result.Should().Contain(r => r.Title == "Pasta");
        result.Should().Contain(r => r.Title == "Salad");

        await _recipeRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSingleRecipe_ShouldReturnSingleRecipeDto()
    {
        // Arrange
        var recipeResult = Recipe.Create(
            "Solo Recipe",
            "Only recipe",
            10,
            20,
            2,
            new List<Ingredient> { Ing("Flour") },
            new List<InstructionStep> { Step("Mix") }
        );

        var recipes = new List<Recipe> { recipeResult.Value };

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(recipes);

        var query = new GetAllRecipesQuery();

        // Act
        IEnumerable<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var dto = result.First();
        dto.Title.Should().Be("Solo Recipe");
        dto.Description.Should().Be("Only recipe");
    }

    [Fact]
    public async Task Handle_WhenNoRecipes_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyRecipes = new List<Recipe>();

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(emptyRecipes);

        var query = new GetAllRecipesQuery();

        // Act
        IEnumerable<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        await _recipeRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMapAllRecipePropertiesCorrectly()
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Ingredient1"), Ing("Ingredient2"), Ing("Ingredient3") };
        var instructions = new List<InstructionStep> { Step("Step1"), Step("Step2"), Step("Step3") };

        var recipeResult = Recipe.Create(
            "Test Recipe",
            "Test Description",
            25,
            35,
            6,
            ingredients,
            instructions
        );

        var recipes = new List<Recipe> { recipeResult.Value };

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(recipes);

        var query = new GetAllRecipesQuery();

        // Act
        IEnumerable<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(recipeResult.Value.Id);
        dto.Title.Should().Be("Test Recipe");
        dto.Description.Should().Be("Test Description");
        dto.PreparationTime.Should().Be(25);
        dto.CookingTime.Should().Be(35);
        dto.Servings.Should().Be(6);
        dto.Ingredients.Select(i => i.Name).Should().Equal("Ingredient1", "Ingredient2", "Ingredient3");
        dto.Instructions.Select(s => s.Text).Should().Equal(instructions.Select(s => s.Text));
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryGetAllAsync()
    {
        // Arrange
        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Recipe>());

        var query = new GetAllRecipesQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _recipeRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Recipe>());

        var query = new GetAllRecipesQuery();
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _recipeRepository.Received(1).GetAllAsync(
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    [Fact]
    public async Task Handle_WithLargeNumberOfRecipes_ShouldReturnAllRecipes()
    {
        // Arrange 
        var recipes = new List<Recipe>();
        for (int i = 0; i < 50; i++)
        {
            var recipeResult = Recipe.Create(
                $"Recipe {i}",
                $"Description {i}",
                10,
                20,
                2,
                new List<Ingredient> { Ing($"Ingredient{i}") },
                new List<InstructionStep> { Step($"Step{i}") }
            );
            recipes.Add(recipeResult.Value);
        }

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(recipes);

        var query = new GetAllRecipesQuery();

        // Act
        IEnumerable<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(50);
    }

    [Fact]
    public async Task Handle_WithRecipesHavingDifferentCookingTimes_ShouldMapCorrectly()
    {
        // Arrange
        var recipe1 = Recipe.Create("Salad", "No cooking", 10, 0, 2,
            new List<Ingredient> { Ing("Lettuce") }, new List<InstructionStep> { Step("Chop") }).Value;

        var recipe2 = Recipe.Create("Frozen Pizza", "No prep", 0, 15, 2,
            new List<Ingredient> { Ing("Pizza") }, new List<InstructionStep> { Step("Bake") }).Value;

        var recipe3 = Recipe.Create("Cake", "Both times", 20, 30, 8,
            new List<Ingredient> { Ing("Flour") }, new List<InstructionStep> { Step("Mix"), Step("Bake") }).Value;

        var recipes = new List<Recipe> { recipe1, recipe2, recipe3 };

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(recipes);

        var query = new GetAllRecipesQuery();

        // Act
        IEnumerable<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.CookingTime == 0);
        result.Should().Contain(r => r.PreparationTime == 0);
        result.Should().Contain(r => r.PreparationTime > 0 && r.CookingTime > 0);
    }

    #endregion
}
