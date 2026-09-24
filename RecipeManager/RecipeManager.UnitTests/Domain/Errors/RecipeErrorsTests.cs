using FluentAssertions;
using FluentResults;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Domain.Errors;

public class RecipeErrorsTests
{
    [Fact]
    public void RecipeNotFound_ShouldBeNotFoundDomainError()
    {
        // Act
        Error error = RecipeErrors.RecipeNotFound(Guid.NewGuid());

        // Assert
        error.Should().BeOfType<DomainError>()
            .Which.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public void InvariantErrors_ShouldBeValidationDomainErrors()
    {
        // Act
        Error[] errors =
        [
            RecipeErrors.TitleRequired(),
            RecipeErrors.DescriptionRequired(),
            RecipeErrors.PreparationTimeNegative(),
            RecipeErrors.CookingTimeNegative(),
            RecipeErrors.BothTimesZero(),
            RecipeErrors.ServingsOutOfRange(1),
            RecipeErrors.IngredientsRequired(),
            RecipeErrors.IngredientNameRequired(),
            RecipeErrors.IngredientQuantityNotPositive(),
            RecipeErrors.IngredientUnitWithoutQuantity(),
            RecipeErrors.InstructionsRequired(),
            RecipeErrors.InstructionEmpty()
        ];

        // Assert
        errors.Should().HaveCount(12).And.AllSatisfy(error =>
            error.Should().BeOfType<DomainError>()
                .Which.Kind.Should().Be(ErrorKind.Validation));
    }
}
