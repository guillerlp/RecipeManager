using FluentAssertions;
using FluentResults;
using NSubstitute;
using RecipeManager.Application.Commands.Cooks;
using RecipeManager.Application.DTO.Cooks;
using RecipeManager.Application.Handlers.Cooks;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.UnitTests.Application.Handlers;

public class LogCookHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 18, 0, 0, TimeSpan.Zero);

    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly ICookRepository _cookRepository = Substitute.For<ICookRepository>();
    private readonly LogCookHandler _handler;
    private readonly Recipe _recipe = Recipe.Create("Cacio e Pepe", null, null, null, null, [], [], RecipeStatus.Draft).Value;

    public LogCookHandlerTests()
    {
        // GetUtcNow is virtual, so a plain substitute pins the clock without a fake-time package.
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(Now);
        _handler = new LogCookHandler(_recipeRepository, _cookRepository, clock);
        _recipeRepository.GetByIdAsync(_recipe.Id, Arg.Any<CancellationToken>()).Returns(_recipe);
    }

    [Fact]
    public async Task Handle_WithNoDate_ShouldStoreACookAtNowAndReturnIt()
    {
        Result<CookDto> result = await _handler.Handle(new LogCookCommand(_recipe.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CookedAt.Should().Be(Now);
        await _cookRepository.Received(1).AddAsync(
            Arg.Is<Cook>(c => c != null && c.RecipeId == _recipe.Id && c.Id == result.Value.Id && c.CookedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheRecipeDoesNotExist_ShouldReturnRecipeNotFoundAndStoreNothing()
    {
        Guid missing = Guid.NewGuid();

        Result<CookDto> result = await _handler.Handle(new LogCookCommand(missing, null), CancellationToken.None);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<DomainError>()
            .Which.Kind.Should().Be(ErrorKind.NotFound);
        await _cookRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithAFutureDate_ShouldReturnTheValidationErrorAndStoreNothing()
    {
        Result<CookDto> result =
            await _handler.Handle(new LogCookCommand(_recipe.Id, Now.AddDays(1)), CancellationToken.None);

        result.Errors.Should().ContainSingle().Which.Metadata["field"].Should().Be("cookedAt");
        await _cookRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldPassTheCancellationTokenToBothRepositories()
    {
        var token = new CancellationTokenSource().Token;

        await _handler.Handle(new LogCookCommand(_recipe.Id, null), token);

        await _recipeRepository.Received(1).GetByIdAsync(_recipe.Id, token);
        await _cookRepository.Received(1).AddAsync(Arg.Any<Cook>(), token);
    }
}
