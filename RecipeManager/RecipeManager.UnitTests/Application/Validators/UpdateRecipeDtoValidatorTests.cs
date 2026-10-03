using FluentValidation.TestHelper;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Validators.Recipes;

namespace RecipeManager.UnitTests.Application.Validators;

public class UpdateRecipeDtoValidatorTests
{
    private readonly UpdateRecipeDtoValidator _validator = new();

    private static UpdateRecipeDto WithTags(List<string> tags) => new("Title", null, null, null, null, [], [], tags);

    [Fact]
    public void Validate_TwentyOneTags_ShouldFail()
    {
        List<string> tags = Enumerable.Range(0, 21).Select(i => $"tag{i}").ToList();

        _validator.TestValidate(WithTags(tags)).ShouldHaveValidationErrorFor(d => d.Tags);
    }

    [Fact]
    public void Validate_AFortyOneCharacterTag_ShouldFail()
    {
        _validator.TestValidate(WithTags([new string('x', 41)])).ShouldHaveValidationErrorFor("Tags[0]");
    }

    [Fact]
    public void Validate_ANullTagList_ShouldFailWithoutThrowing()
    {
        _validator.TestValidate(WithTags(null!)).ShouldHaveValidationErrorFor(d => d.Tags);
    }
}
