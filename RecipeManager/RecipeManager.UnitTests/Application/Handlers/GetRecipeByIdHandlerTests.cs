using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

/// <summary>
/// Unit tests for GetRecipeByIdHandler
/// </summary>
public class GetRecipeByIdHandlerTests
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly GetRecipeByIdHandler _handler;

    public GetRecipeByIdHandlerTests()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _handler = new GetRecipeByIdHandler(_recipeRepository, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetRecipeByIdHandler>>());
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnRecipeDto()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var existingRecipeResult = Recipe.Create(
            "Chocolate Cake",
            "Delicious chocolate cake",
            20,
            30,
            8,
            new List<string> { "Flour", "Sugar", "Cocoa" },
            new List<string> { "Mix", "Bake" }
        );
        var existingRecipe = existingRecipeResult.Value;

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipe);

        var query = new GetRecipeByIdQuery(recipeId);

        // Act
        Result<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(existingRecipe.Id);
        result.Value.Title.Should().Be(existingRecipe.Title);
        result.Value.Description.Should().Be(existingRecipe.Description);
        result.Value.PreparationTime.Should().Be(existingRecipe.PreparationTime);
        result.Value.CookingTime.Should().Be(existingRecipe.CookingTime);
        result.Value.Servings.Should().Be(existingRecipe.Servings);
        result.Value.Ingredients.Should().BeEquivalentTo(existingRecipe.Ingredients);
        result.Value.Instructions.Should().BeEquivalentTo(existingRecipe.Instructions);

        await _recipeRepository.Received(1).GetByIdAsync(recipeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldCallRepositoryGetByIdAsync()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var existingRecipeResult = Recipe.Create(
            "Recipe",
            "Description",
            10,
            20,
            2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        var query = new GetRecipeByIdQuery(recipeId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _recipeRepository.Received(1).GetByIdAsync(recipeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnDtoWithAllProperties()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var ingredients = new List<string> { "Flour", "Sugar", "Eggs", "Butter" };
        var instructions = new List<string> { "Mix dry ingredients", "Add wet ingredients", "Bake" };

        var existingRecipeResult = Recipe.Create(
            "Complex Recipe",
            "A recipe with many ingredients",
            15,
            45,
            4,
            ingredients,
            instructions
        );

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        var query = new GetRecipeByIdQuery(recipeId);

        // Act
        Result<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ingredients.Should().HaveCount(4);
        result.Value.Instructions.Should().HaveCount(3);
        result.Value.Ingredients.Should().BeEquivalentTo(ingredients);
        result.Value.Instructions.Should().BeEquivalentTo(instructions);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenRecipeNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        var recipeId = Guid.NewGuid();

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        var query = new GetRecipeByIdQuery(recipeId);

        // Act
        Result<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenRecipeNotFound_ShouldIncludeRecipeIdInError()
    {
        // Arrange
        var recipeId = Guid.NewGuid();

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        var query = new GetRecipeByIdQuery(recipeId);

        // Act
        Result<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Contain(recipeId.ToString());
    }

    [Fact]
    public async Task Handle_WhenRecipeNotFound_ShouldIncludeErrorMetadata()
    {
        // Arrange
        var recipeId = Guid.NewGuid();

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        var query = new GetRecipeByIdQuery(recipeId);

        // Act
        Result<RecipeDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.First();
        error.Metadata.Should().ContainKey("ErrorCode");
        error.Metadata["ErrorCode"].Should().Be(404); 
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var existingRecipeResult = Recipe.Create(
            "Recipe",
            "Description",
            10,
            20,
            2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipeResult.Value);

        var query = new GetRecipeByIdQuery(recipeId);
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _recipeRepository.Received(1).GetByIdAsync(
            recipeId,
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    #endregion
}