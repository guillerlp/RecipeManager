using FluentAssertions;
using FluentResults;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Domain.Entities;

public class CookTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid RecipeId = Guid.NewGuid();

    [Fact]
    public void Create_WithNoDate_ShouldUseNowAndMintAnId()
    {
        Result<Cook> result = Cook.Create(RecipeId, null, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.RecipeId.Should().Be(RecipeId);
        result.Value.CookedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_WithAPastDate_ShouldKeepIt()
    {
        DateTimeOffset sunday = Now.AddDays(-6);

        Cook.Create(RecipeId, sunday, Now).Value.CookedAt.Should().Be(sunday);
    }

    [Fact]
    public void Create_WithExactlyNow_ShouldSucceed()
    {
        Cook.Create(RecipeId, Now, Now).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithAFutureDate_ShouldFailOnCookedAt()
    {
        Result<Cook> result = Cook.Create(RecipeId, Now.AddSeconds(1), Now);

        result.IsFailed.Should().BeTrue();
        IError error = result.Errors.Should().ContainSingle().Subject;
        error.Should().BeOfType<DomainError>().Which.Kind.Should().Be(ErrorKind.Validation);
        error.Metadata["field"].Should().Be("cookedAt");
    }

    [Fact]
    public void Create_WithANonUtcOffset_ShouldStoreTheSameInstantInUtc()
    {
        // Npgsql writes timestamptz only from an offset-zero DateTimeOffset; any other offset throws at SaveChanges.
        var kolkata = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(5.5));

        Cook cook = Cook.Create(RecipeId, kolkata, Now).Value;

        cook.CookedAt.Offset.Should().Be(TimeSpan.Zero);
        cook.CookedAt.Should().Be(kolkata);
        cook.CookedAt.UtcDateTime.Hour.Should().Be(6);
    }

    [Fact]
    public void CookNotFound_ShouldBeANotFoundErrorOnId()
    {
        Guid id = Guid.NewGuid();

        IError error = CookErrors.CookNotFound(id);

        error.Should().BeOfType<DomainError>().Which.Kind.Should().Be(ErrorKind.NotFound);
        error.Message.Should().Contain(id.ToString());
        error.Metadata["field"].Should().Be("id");
    }
}
