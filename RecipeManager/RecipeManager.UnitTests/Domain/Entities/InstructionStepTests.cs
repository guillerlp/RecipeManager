using FluentAssertions;
using FluentResults;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Domain.Entities;

public class InstructionStepTests
{
    [Fact]
    public void Create_WithValidData_ShouldKeepEveryValueAndMintAnId()
    {
        Guid flour = Guid.NewGuid();
        Guid butter = Guid.NewGuid();

        Result<InstructionStep> result = InstructionStep.Create("Rub in", 5, [butter, flour]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.Text.Should().Be("Rub in");
        result.Value.DurationMinutes.Should().Be(5);
        // Order is part of the value: "for this step" lists ingredients in the order the author gave.
        result.Value.IngredientIds.Should().Equal(butter, flour);
    }

    [Fact]
    public void Create_WithNoDurationAndNoReferences_ShouldSucceed()
    {
        // Most real steps have neither; both being optional is also what makes the text[] backfill lossless.
        Result<InstructionStep> result = InstructionStep.Create("Serve", null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.DurationMinutes.Should().BeNull();
        result.Value.IngredientIds.Should().BeEmpty();
    }

    [Fact]
    public void Create_TwiceWithTheSameInput_ShouldMintDistinctIds()
    {
        InstructionStep a = InstructionStep.Create("Mix", null, []).Value;
        InstructionStep b = InstructionStep.Create("Mix", null, []).Value;

        a.Id.Should().NotBe(b.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankText_ShouldFailWithInstructionTextRequired(string? text)
    {
        Result<InstructionStep> result = InstructionStep.Create(text!, null, []);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.InstructionTextRequired().Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveDuration_ShouldFailWithInstructionDurationNotPositive(int duration)
    {
        Result<InstructionStep> result = InstructionStep.Create("Rest", duration, []);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.InstructionDurationNotPositive().Message);
    }

    [Fact]
    public void Create_WithBlankTextAndZeroDuration_ShouldReportBothErrors()
    {
        Result<InstructionStep> result = InstructionStep.Create(" ", 0, []);

        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void IngredientIds_ShouldNotBeCastableToAMutableList()
    {
        // BUG-11's lesson applied from the start: a bare ToList() would be castable back to List<Guid>.
        InstructionStep step = InstructionStep.Create("Mix", null, [Guid.NewGuid()]).Value;

        step.IngredientIds.Should().NotBeAssignableTo<List<Guid>>();
    }

    [Fact]
    public void Create_ShouldCopyTheReferences_SoLaterChangesToTheCallersListDoNotLeakIn()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        InstructionStep step = InstructionStep.Create("Mix", null, ids).Value;

        ids.Add(Guid.NewGuid());

        step.IngredientIds.Should().HaveCount(1);
    }
}
