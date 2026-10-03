using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class UnpublishRecipeHandlerTests
{
    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly UnpublishRecipeHandler _handler;

    public UnpublishRecipeHandlerTests()
    {
        _handler = new UnpublishRecipeHandler(_recipeRepository);
    }

    [Fact]
    public async Task Handle_PublishedRecipe_ShouldReturnToDraftAndSave()
    {
        Recipe recipe = Recipe.Create("Title", "Description", 10, 20, 4,
            [Ingredient.Create(null, null, null, "Flour", null).Value],
            [InstructionStep.Create("Mix", null, []).Value]).Value;
        using var cts = new CancellationTokenSource();
        _recipeRepository.GetByIdForUpdateAsync(recipe.Id, cts.Token).Returns(recipe);

        Result result = await _handler.Handle(new UnpublishRecipeCommand(recipe.Id), cts.Token);

        result.IsSuccess.Should().BeTrue();
        recipe.Status.Should().Be(RecipeStatus.Draft);
        await _recipeRepository.Received(1).UpdateAsync(recipe, cts.Token);
    }

    [Fact]
    public async Task Handle_UnknownId_ShouldReturnNotFoundWithoutSaving()
    {
        var id = Guid.NewGuid();
        _recipeRepository.GetByIdForUpdateAsync(id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        Result result = await _handler.Handle(new UnpublishRecipeCommand(id), CancellationToken.None);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<DomainError>()
            .Which.Kind.Should().Be(ErrorKind.NotFound);
        await _recipeRepository.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }
}
