using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.Commands.Cooks;
using RecipeManager.Application.Handlers.Cooks;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class DeleteCookHandlerTests
{
    private readonly ICookRepository _cookRepository = Substitute.For<ICookRepository>();
    private readonly DeleteCookHandler _handler;

    public DeleteCookHandlerTests()
    {
        _handler = new DeleteCookHandler(_cookRepository);
    }

    [Fact]
    public async Task Handle_WhenTheCookExists_ShouldDeleteIt()
    {
        Guid recipeId = Guid.NewGuid();
        Cook cook = Cook.Create(recipeId, null, DateTimeOffset.UtcNow).Value;
        _cookRepository.GetByIdAsync(recipeId, cook.Id, Arg.Any<CancellationToken>()).Returns(cook);

        Result result = await _handler.Handle(new DeleteCookCommand(recipeId, cook.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _cookRepository.Received(1).DeleteAsync(cook, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheCookIsNotThisRecipes_ShouldReturnCookNotFound()
    {
        Guid cookId = Guid.NewGuid();
        _cookRepository.GetByIdAsync(Arg.Any<Guid>(), cookId, Arg.Any<CancellationToken>()).Returns((Cook?)null);

        Result result = await _handler.Handle(new DeleteCookCommand(Guid.NewGuid(), cookId), CancellationToken.None);

        IError error = result.Errors.Should().ContainSingle().Subject;
        error.Should().BeOfType<DomainError>().Which.Kind.Should().Be(ErrorKind.NotFound);
        error.Message.Should().Contain(cookId.ToString());
        await _cookRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
