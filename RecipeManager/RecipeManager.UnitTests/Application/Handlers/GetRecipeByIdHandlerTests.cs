using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

/// <summary>
/// Unit tests for GetRecipeByIdHandler
/// </summary>
public class GetRecipeByIdHandlerTests
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICookRepository _cookRepository;
    private readonly GetRecipeByIdHandler _handler;

    public GetRecipeByIdHandlerTests()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _cookRepository = Substitute.For<ICookRepository>();
        _cookRepository.GetStatsAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, CookStats>());
        _handler = new GetRecipeByIdHandler(_recipeRepository, _cookRepository,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<GetRecipeByIdHandler>>());
    }

    private static Ingredient Ing(string name) => Ingredient.Create(null, null, null, name, null).Value;

    private static InstructionStep Step(string text) => InstructionStep.Create(text, null, []).Value;

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
            new List<Ingredient> { Ing("Flour"), Ing("Sugar"), Ing("Cocoa") },
            new List<InstructionStep> { Step("Mix"), Step("Bake") }
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
        result.Value.Ingredients.Select(i => i.Name).Should().Equal("Flour", "Sugar", "Cocoa");
        result.Value.Instructions.Select(s => s.Id).Should().Equal(existingRecipe.Instructions.Select(s => s.Id));

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
            new List<Ingredient> { Ing("Flour") },
            new List<InstructionStep> { Step("Mix") }
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
        var ingredients = new List<Ingredient> { Ing("Flour"), Ing("Sugar"), Ing("Eggs"), Ing("Butter") };
        var instructions = new List<InstructionStep> { Step("Mix dry ingredients"), Step("Add wet ingredients"), Step("Bake") };

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
        result.Value.Ingredients.Select(i => i.Name).Should().Equal("Flour", "Sugar", "Eggs", "Butter");
        result.Value.Instructions.Select(s => s.Text).Should().Equal(instructions.Select(s => s.Text));
    }

    [Fact]
    public async Task Handle_ShouldCarryTheRecipesCookStats()
    {
        Recipe recipe = Recipe.Create("Cooked", null, null, null, null, [], [], RecipeStatus.Draft).Value;
        var last = new DateTimeOffset(2026, 10, 8, 19, 0, 0, TimeSpan.Zero);
        _recipeRepository.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        _cookRepository.GetStatsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == recipe.Id),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, CookStats> { [recipe.Id] = new(11, last) });

        Result<RecipeDto> result = await _handler.Handle(new GetRecipeByIdQuery(recipe.Id), CancellationToken.None);

        result.Value.CookCount.Should().Be(11);
        result.Value.LastCookedAt.Should().Be(last);
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
    public async Task Handle_WhenRecipeNotFound_ShouldReturnNotFoundKindWithoutHttpCode()
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
        error.Should().BeOfType<DomainError>()
            .Which.Kind.Should().Be(ErrorKind.NotFound);
        error.Metadata["field"].Should().Be("id");
        error.Metadata.Should().NotContainKey("ErrorCode");
    }

    [Fact]
    public async Task Handle_WhenRecipeNotFound_ShouldNotQueryCooks()
    {
        _recipeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        await _handler.Handle(new GetRecipeByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await _cookRepository.DidNotReceiveWithAnyArgs().GetStatsAsync(default!, default);
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
            new List<Ingredient> { Ing("Flour") },
            new List<InstructionStep> { Step("Mix") }
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
