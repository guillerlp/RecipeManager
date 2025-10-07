using FluentAssertions;
using FluentResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class DeleteRecipeHandlerTests
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly DeleteRecipeHandler _handler;

    public DeleteRecipeHandlerTests()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeleteRecipeHandler>>();
        _handler = new DeleteRecipeHandler(_recipeRepository, logger);
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnSuccessAndDeleteRecipe()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var existingRecipeResult = Recipe.Create(
            "Recipe to Delete",
            "Description",
            10,
            20,
            2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );
        var existingRecipe = existingRecipeResult.Value;

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns(existingRecipe);

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _recipeRepository.Received(1).GetByIdAsync(recipeId, Arg.Any<CancellationToken>());

        await _recipeRepository.Received(1).DeleteAsync(
            Arg.Is<Recipe>(r => r.Id == existingRecipe.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldCallRepositoryDeleteAsync()
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

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _recipeRepository.Received(1).DeleteAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
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

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Message.Should().Contain("not found");

        await _recipeRepository.DidNotReceive().DeleteAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRecipeNotFound_ShouldIncludeRecipeIdInError()
    {
        // Arrange
        var recipeId = Guid.NewGuid();

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Contain(recipeId.ToString());
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailureResult()
    {
        // Arrange
        var recipeId = Guid.NewGuid();

        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Error while deleting the recipe"));
        result.Errors.Should().Contain(e => e.Message.Contains("Database connection failed"));
    }

    [Fact]
    public async Task Handle_WhenDeleteAsyncThrowsException_ShouldReturnFailureResult()
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

        _recipeRepository.DeleteAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Failed to delete from database"));

        var command = new DeleteRecipeCommand(recipeId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Error while deleting the recipe"));
        result.Errors.Should().Contain(e => e.Message.Contains("Failed to delete from database"));
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

        var command = new DeleteRecipeCommand(recipeId);
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert 
        await _recipeRepository.Received(1).GetByIdAsync(
            recipeId,
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));

        await _recipeRepository.Received(1).DeleteAsync(
            Arg.Any<Recipe>(),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    #endregion
}