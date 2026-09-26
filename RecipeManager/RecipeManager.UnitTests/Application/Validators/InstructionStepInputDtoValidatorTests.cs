using FluentValidation.TestHelper;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Validators.Recipes;

namespace RecipeManager.UnitTests.Application.Validators;

public class InstructionStepInputDtoValidatorTests
{
    private readonly InstructionStepInputDtoValidator _validator = new();

    [Fact]
    public void Validate_AValidStep_ShouldPass()
    {
        _validator.TestValidate(new InstructionStepInputDto("Mix", 5, [0, 1])).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TextAtTheCap_ShouldPass()
    {
        _validator.TestValidate(new InstructionStepInputDto(new string('x', 2000), null, []))
            .ShouldNotHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Validate_TextOverTheCap_ShouldFail()
    {
        _validator.TestValidate(new InstructionStepInputDto(new string('x', 2001), null, []))
            .ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Validate_NullText_ShouldFail()
    {
        _validator.TestValidate(new InstructionStepInputDto(null!, null, []))
            .ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Theory]
    [InlineData(0)]    // passes here on purpose: "positive" is the domain's rule (422), not payload shape
    [InlineData(1439)]
    public void Validate_DurationInsideTheBound_ShouldPass(int duration)
    {
        _validator.TestValidate(new InstructionStepInputDto("Rest", duration, []))
            .ShouldNotHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1440)]
    public void Validate_DurationOutsideTheBound_ShouldFail(int duration)
    {
        _validator.TestValidate(new InstructionStepInputDto("Rest", duration, []))
            .ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Fact]
    public void Validate_NegativeIndex_ShouldFail()
    {
        _validator.TestValidate(new InstructionStepInputDto("Mix", null, [-1]))
            .ShouldHaveValidationErrorFor(x => x.IngredientIndexes);
    }

    [Fact]
    public void Validate_DuplicateIndexes_ShouldFail()
    {
        _validator.TestValidate(new InstructionStepInputDto("Mix", null, [0, 0]))
            .ShouldHaveValidationErrorFor(x => x.IngredientIndexes);
    }

    [Fact]
    public void Validate_MoreThan50Indexes_ShouldFail()
    {
        _validator.TestValidate(new InstructionStepInputDto("Mix", null, [.. Enumerable.Range(0, 51)]))
            .ShouldHaveValidationErrorFor(x => x.IngredientIndexes);
    }

    [Fact]
    public void Validate_NullIndexes_ShouldFailWithoutThrowing()
    {
        _validator.TestValidate(new InstructionStepInputDto("Mix", null, null!))
            .ShouldHaveValidationErrorFor(x => x.IngredientIndexes);
    }
}
