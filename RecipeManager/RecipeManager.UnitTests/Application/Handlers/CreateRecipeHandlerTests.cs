using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class CreateRecipeHandlerTests
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly CreateRecipeHandler _handler;

    public CreateRecipeHandlerTests()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _handler = new CreateRecipeHandler(_recipeRepository);
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResultWithRecipeDto()
    {
        // Arrange 
        var command = new CreateRecipeCommand(
            Title: "Chocolate Cake",
            Description: "Delicious chocolate cake",
            PreparationTime: 20,
            CookingTime: 30,
            Servings: 8,
            Ingredients: new List<string> { "Flour", "Sugar", "Cocoa" },
            Instructions: new List<string> { "Mix", "Bake" }
        );

        // Act 
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert 
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be(command.Title);
        result.Value.Description.Should().Be(command.Description);
        result.Value.PreparationTime.Should().Be(command.PreparationTime);
        result.Value.CookingTime.Should().Be(command.CookingTime);
        result.Value.Servings.Should().Be(command.Servings);
        result.Value.Ingredients.Should().BeEquivalentTo(command.Ingredients);
        result.Value.Instructions.Should().BeEquivalentTo(command.Instructions);
        result.Value.Id.Should().NotBeEmpty();

        await _recipeRepository.Received(1).AddAsync(
            Arg.Is<Recipe>(r => r.Title == command.Title),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallRepositoryAddAsync()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            "Pasta", "Italian pasta", 10, 15, 4,
            new List<string> { "Pasta", "Tomatoes" },
            new List<string> { "Boil", "Mix" }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert 
        await _recipeRepository.Received(1).AddAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPassCorrectRecipeToRepository()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            "Test Recipe", "Test Description", 5, 10, 2,
            new List<string> { "Ingredient1" },
            new List<string> { "Step1" }
        );

        Recipe? capturedRecipe = null;

        await _recipeRepository.AddAsync(
            Arg.Do<Recipe>(r => capturedRecipe = r),
            Arg.Any<CancellationToken>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert 
        capturedRecipe.Should().NotBeNull();
        capturedRecipe!.Title.Should().Be(command.Title);
        capturedRecipe.Description.Should().Be(command.Description);
        capturedRecipe.PreparationTime.Should().Be(command.PreparationTime);
        capturedRecipe.CookingTime.Should().Be(command.CookingTime);
        capturedRecipe.Servings.Should().Be(command.Servings);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithInvalidTitle_ShouldReturnFailureResult()
    {
        // Arrange 
        var command = new CreateRecipeCommand(
            Title: "",
            Description: "Valid description",
            PreparationTime: 10,
            CookingTime: 20,
            Servings: 2,
            Ingredients: new List<string> { "Flour" },
            Instructions: new List<string> { "Mix" }
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Message.Contains("Title"));

        await _recipeRepository.DidNotReceive().AddAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            Title: "",
            Description: "",
            PreparationTime: -5,
            CookingTime: -10,
            Servings: 0,
            Ingredients: new List<string>(),
            Instructions: new List<string>()
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1);

        await _recipeRepository.DidNotReceive().AddAsync(
            Arg.Any<Recipe>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyIngredients_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            "Valid Title", "Valid Description", 10, 20, 2,
            Ingredients: new List<string>(),
            Instructions: new List<string> { "Step1" }
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("ingredient"));
    }

    [Fact]
    public async Task Handle_WithEmptyInstructions_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            "Valid Title", "Valid Description", 10, 20, 2,
            Ingredients: new List<string> { "Flour" },
            Instructions: new List<string>()
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("instruction"));
    }

    [Fact]
    public async Task Handle_WithBothTimesZero_ShouldReturnFailureResult()
    {
        // Arrange 
        var command = new CreateRecipeCommand(
            "Valid Title", "Valid Description",
            PreparationTime: 0, // Both zero is invalid
            CookingTime: 0,
            Servings: 2,
            Ingredients: new List<string> { "Flour" },
            Instructions: new List<string> { "Mix" }
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Message.Contains("At least one of preparation or cooking time must be greater than 0"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            "Title", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _recipeRepository.Received(1).AddAsync(
            Arg.Any<Recipe>(),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    [Fact]
    public async Task Handle_WithOnlyPreparationTime_ShouldSucceed()
    {
        // Arrange - Edge case: only prep time, no cooking time
        var command = new CreateRecipeCommand(
            "Salad", "Fresh salad",
            PreparationTime: 10,
            CookingTime: 0, // No cooking needed
            Servings: 2,
            Ingredients: new List<string> { "Lettuce" },
            Instructions: new List<string> { "Chop" }
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithOnlyCookingTime_ShouldSucceed()
    {
        // Arrange - Edge case: only cooking time, no prep time
        var command = new CreateRecipeCommand(
            "Frozen Pizza", "Heat up pizza",
            PreparationTime: 0, // No prep needed
            CookingTime: 15,
            Servings: 2,
            Ingredients: new List<string> { "Frozen pizza" },
            Instructions: new List<string> { "Bake" }
        );

        // Act
        Result<RecipeDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}