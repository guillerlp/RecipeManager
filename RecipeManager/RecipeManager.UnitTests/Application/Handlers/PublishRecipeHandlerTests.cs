using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Handlers.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class PublishRecipeHandlerTests
{
    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly PublishRecipeHandler _handler;

    public PublishRecipeHandlerTests()
    {
        _handler = new PublishRecipeHandler(_recipeRepository);
    }

    private static Recipe Draft(bool complete) => complete
        ? Recipe.Create("Title", "Description", 10, 20, 4,
            [Ingredient.Create(null, null, null, "Flour", null).Value],
            [InstructionStep.Create("Mix", null, []).Value], RecipeStatus.Draft).Value
        : Recipe.Create("Half-written", null, null, null, null, [], [], RecipeStatus.Draft).Value;

    [Fact]
    public async Task Handle_CompleteDraft_ShouldPublishAndSave()
    {
        Recipe draft = Draft(complete: true);
        _recipeRepository.GetByIdForUpdateAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        Result result = await _handler.Handle(new PublishRecipeCommand(draft.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(RecipeStatus.Published);
        await _recipeRepository.Received(1).UpdateAsync(draft, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncompleteDraft_ShouldFailWithoutSaving()
    {
        Recipe draft = Draft(complete: false);
        _recipeRepository.GetByIdForUpdateAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        Result result = await _handler.Handle(new PublishRecipeCommand(draft.Id), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        await _recipeRepository.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ShouldReturnNotFoundWithoutSaving()
    {
        var id = Guid.NewGuid();
        _recipeRepository.GetByIdForUpdateAsync(id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        Result result = await _handler.Handle(new PublishRecipeCommand(id), CancellationToken.None);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<DomainError>()
            .Which.Kind.Should().Be(ErrorKind.NotFound);
        await _recipeRepository.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassTheCancellationTokenThrough()
    {
        Recipe draft = Draft(complete: true);
        using var cts = new CancellationTokenSource();
        _recipeRepository.GetByIdForUpdateAsync(draft.Id, cts.Token).Returns(draft);

        await _handler.Handle(new PublishRecipeCommand(draft.Id), cts.Token);

        await _recipeRepository.Received(1).GetByIdForUpdateAsync(draft.Id, cts.Token);
        await _recipeRepository.Received(1).UpdateAsync(draft, cts.Token);
    }
}
