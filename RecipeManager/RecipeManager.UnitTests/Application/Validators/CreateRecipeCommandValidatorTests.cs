using FluentValidation.TestHelper;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Validators.Recipes;

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
            [null!]);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Instructions[0]");
    }
}
