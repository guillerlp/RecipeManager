using FluentValidation.TestHelper;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Validators.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.UnitTests.Application.Validators;

public class CreateRecipeCommandValidatorTests
{
    private readonly CreateRecipeCommandValidator _validator = new();

    [Fact]
    public void Validate_ANullInstructionStep_ShouldFailInsteadOfReachingTheHandler()
    {
        // JSON allows "instructions": [null]. A child validator skips null items, and MVC's implicit-required
        // check covers properties, not list elements — so without an explicit NotNull the null reaches
        // ToInstructionSteps and becomes a NullReferenceException (500) instead of a 400.
        var command = new CreateRecipeCommand("Title", "Description", 10, 0, 1,
            [new IngredientInputDto(null, null, null, "Flour", null)],
            [null!], []);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Instructions[0]");
    }

    [Fact]
    public void Validate_ADraftWithOnlyATitle_ShouldPass()
    {
        var command = new CreateRecipeCommand("Title", null, null, null, null, [], [], [], RecipeStatus.Draft);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    // Bounds are shape, so they still apply to a value that is present — in a draft too.
    [Theory]
    [InlineData(-1, null, null, "PreparationTime")]
    [InlineData(null, 1440, null, "CookingTime")]
    [InlineData(null, null, 0, "Servings")]
    public void Validate_APresentValueOutOfBounds_ShouldFail(int? prep, int? cook, int? servings, string property)
    {
        var command = new CreateRecipeCommand("Title", null, prep, cook, servings, [], [], [], RecipeStatus.Draft);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(property);
    }

    private static CreateRecipeCommand WithTags(List<string> tags) =>
        new("Title", null, null, null, null, [], [], tags, RecipeStatus.Draft);

    [Fact]
    public void Validate_TwentyTagsOfFortyCharacters_ShouldPass()
    {
        List<string> tags = Enumerable.Range(0, 20).Select(i => $"{i:D2}{new string('x', 38)}").ToList();

        _validator.TestValidate(WithTags(tags)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TwentyOneTags_ShouldFail()
    {
        List<string> tags = Enumerable.Range(0, 21).Select(i => $"tag{i}").ToList();

        _validator.TestValidate(WithTags(tags)).ShouldHaveValidationErrorFor(c => c.Tags);
    }

    [Fact]
    public void Validate_AFortyOneCharacterTag_ShouldFail()
    {
        _validator.TestValidate(WithTags([new string('x', 41)])).ShouldHaveValidationErrorFor("Tags[0]");
    }

    [Fact]
    public void Validate_ANullTag_ShouldFailInsteadOfReachingTheDomain()
    {
        _validator.TestValidate(WithTags([null!])).ShouldHaveValidationErrorFor("Tags[0]");
    }

    [Fact]
    public void Validate_ANullTagList_ShouldFailWithoutThrowing()
    {
        _validator.TestValidate(WithTags(null!)).ShouldHaveValidationErrorFor(c => c.Tags);
    }
}
