using FluentAssertions;
using FluentResults;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Application.Mappings;

public class InstructionMappingExtensionsTests
{
    private static readonly List<Ingredient> Ingredients =
    [
        Ingredient.Create(null, null, null, "Flour", null).Value,
        Ingredient.Create(null, null, null, "Butter", null).Value,
    ];

    [Fact]
    public void ToInstructionSteps_ShouldResolveEachIndexToTheIdAtThatPositionInTheGivenOrder()
    {
        List<InstructionStepInputDto> inputs = [new("Rub in", 5, [1, 0])];

        Result<List<InstructionStep>> result = inputs.ToInstructionSteps(Ingredients);

        result.IsSuccess.Should().BeTrue();
        InstructionStep step = result.Value.Single();
        step.Text.Should().Be("Rub in");
        step.DurationMinutes.Should().Be(5);
        step.IngredientIds.Should().Equal(Ingredients[1].Id, Ingredients[0].Id);
    }

    [Fact]
    public void ToInstructionSteps_ShouldKeepStepOrder()
    {
        List<InstructionStepInputDto> inputs = [new("A", null, []), new("B", null, []), new("C", null, [])];

        Result<List<InstructionStep>> result = inputs.ToInstructionSteps(Ingredients);

        result.Value.Select(s => s.Text).Should().Equal("A", "B", "C");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void ToInstructionSteps_WithAnOutOfRangeIndex_ShouldFailWithInstructionIngredientNotFound(int index)
    {
        // -1 never arrives over HTTP (the validator rejects it), but the handler is callable without the
        // validator, so the mapper must not throw ArgumentOutOfRangeException on it.
        List<InstructionStepInputDto> inputs = [new("Sift", null, [index])];

        Result<List<InstructionStep>> result = inputs.ToInstructionSteps(Ingredients);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.InstructionIngredientNotFound().Message);
    }

    [Fact]
    public void ToInstructionSteps_WithABadIndexAndBlankText_ShouldReportBoth()
    {
        List<InstructionStepInputDto> inputs = [new("  ", null, [9])];

        Result<List<InstructionStep>> result = inputs.ToInstructionSteps(Ingredients);

        result.Errors.Select(e => e.Message).Should().BeEquivalentTo(
            RecipeErrors.InstructionIngredientNotFound().Message,
            RecipeErrors.InstructionTextRequired().Message);
    }

    [Fact]
    public void ToInstructionSteps_ShouldCollectFailuresAcrossSteps()
    {
        List<InstructionStepInputDto> inputs = [new("", null, []), new("Ok", 0, [])];

        Result<List<InstructionStep>> result = inputs.ToInstructionSteps(Ingredients);

        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void ToInstructionSteps_WithNullInput_ShouldReturnAnEmptyListForTheDomainToReject()
    {
        Result<List<InstructionStep>> result = ((List<InstructionStepInputDto>?)null).ToInstructionSteps(Ingredients);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
