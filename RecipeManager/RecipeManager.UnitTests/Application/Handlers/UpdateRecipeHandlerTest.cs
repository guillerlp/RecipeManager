using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class UpdateRecipeHandlerTest
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly UpdateRecipeHandler _handler;

    public UpdateRecipeHandlerTest()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _handler = new UpdateRecipeHandler(_recipeRepository);
    }

    private static Ingredient Ing(string name) => Ingredient.Create(null, null, null, name, null).Value;

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();
        Result<Recipe> existingRecipeResult = Recipe.Create(
            "Original Title",
            "Original Description",
            10,
            20,
            2,
            [Ing("Flour")],
            new List<string> { "Mix" }
        );

        Recipe? existingRecipe = existingRecipeResult.Value;

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipe);

        UpdateRecipeCommand command = new(
            recipeId,
            "Updated Title",
            "Updated Description",
            15,
            25,
            4,
            [
                new IngredientInputDto(null, null, null, "Sugar", null),
                new IngredientInputDto(null, null, null, "Butter", null)
            ],
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _recipeRepository.Received(1).UpdateAsync(
            Arg.Is<Recipe>(r =>
                r != null &&
                r.Title == command.Title &&
                r.Description == command.Description &&
                r.PreparationTime == command.PreparationTime &&
                r.CookingTime == command.CookingTime &&
                r.Servings == command.Servings
            ),
            Arg.Any<CancellationToken>());

        await _recipeRepository.Received(1).GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenRecipeNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        UpdateRecipeCommand command = new(
            recipeId,
            "Updated Title",
            "Updated Description",
            15,
            25,
            4,
            [
                new IngredientInputDto(null, null, null, "Sugar", null),
                new IngredientInputDto(null, null, null, "Butter", null)
            ],
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain(e => e.Message.Contains("not found"));

        await _recipeRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidTitle_ShouldReturnFailureResult()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();
        Result<Recipe> existingRecipeResult = Recipe.Create(
            "Original Title",
            "Original Description",
            10,
            20,
            2,
            [Ing("Flour")],
            new List<string> { "Mix" }
        );

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        UpdateRecipeCommand command = new(
            recipeId,
            "", // Invalid: empty title
            "Updated Description",
            15,
            25,
            4,
            [
                new IngredientInputDto(null, null, null, "Sugar", null),
                new IngredientInputDto(null, null, null, "Butter", null)
            ],
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Title"));

        // UpdateAsync should NOT be called when validation fails
        await _recipeRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyIngredients_ShouldReturnFailureResult()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();
        Result<Recipe> existingRecipeResult = Recipe.Create(
            "Original Title",
            "Original Description",
            10,
            20,
            2,
            [Ing("Flour")],
            new List<string> { "Mix" }
        );

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        UpdateRecipeCommand command = new(
            recipeId,
            "Updated Title",
            "Updated Description",
            15,
            25,
            4,
            new List<IngredientInputDto>(), // Invalid: empty ingredients
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("ingredient"));
    }

    [Fact]
    public async Task Handle_WithInvalidIngredient_ShouldFailWithoutCallingTheRepository()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();
        Result<Recipe> existingRecipeResult = Recipe.Create(
            "Original Title",
            "Original Description",
            10,
            20,
            2,
            [Ing("Flour")],
            new List<string> { "Mix" }
        );

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        UpdateRecipeCommand command = new(
            recipeId,
            "Updated Title",
            "Updated Description",
            15,
            25,
            4,
            [new IngredientInputDto(null, null, Unit.Gram, "Sugar", null)],
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();

        await _recipeRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region EdgeCases

    [Fact]
    public async Task Handle_WithOnlyPreparationTime_ShouldSucceed()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();
        Result<Recipe> existingRecipeResult = Recipe.Create(
            "Original Title",
            "Original Description",
            10,
            20,
            2,
            [Ing("Flour")],
            new List<string> { "Mix" }
        );

        Recipe? existingRecipe = existingRecipeResult.Value;

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipe);

        UpdateRecipeCommand command = new(
            recipeId,
            "Updated Title",
            "Updated Description",
            15,
            0,
            4,
            [
                new IngredientInputDto(null, null, null, "Sugar", null),
                new IngredientInputDto(null, null, null, "Butter", null)
            ],
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _recipeRepository.Received(1).UpdateAsync(
            Arg.Is<Recipe>(r =>
                r != null &&
                r.PreparationTime == command.PreparationTime &&
                r.CookingTime == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOnlyCookingTime_ShouldSucceed()
    {
        // Arrange
        Guid recipeId = Guid.NewGuid();
        Result<Recipe> existingRecipeResult = Recipe.Create(
            "Original Title",
            "Original Description",
            10,
            20,
            2,
            [Ing("Flour")],
            new List<string> { "Mix" }
        );

        _recipeRepository.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        UpdateRecipeCommand command = new(
            recipeId,
            "Updated Title",
            "Updated Description",
            0, // Only cooking time
            30,
            4,
            [
                new IngredientInputDto(null, null, null, "Sugar", null),
                new IngredientInputDto(null, null, null, "Butter", null)
            ],
            new List<string> { "Cream", "Mix" }
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _recipeRepository.Received(1).UpdateAsync(
            Arg.Is<Recipe>(r =>
                r != null &&
                r.PreparationTime == 0 &&
                r.CookingTime == command.CookingTime),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
