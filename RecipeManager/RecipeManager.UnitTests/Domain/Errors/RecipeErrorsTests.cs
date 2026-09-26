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
            RecipeErrors.InstructionTextRequired(),
            RecipeErrors.InstructionDurationNotPositive(),
            RecipeErrors.InstructionIngredientNotFound()
        ];

        // Assert
        errors.Should().HaveCount(14).And.AllSatisfy(error =>
            error.Should().BeOfType<DomainError>()
                .Which.Kind.Should().Be(ErrorKind.Validation));
    }

    [Fact]
    public void IngredientErrors_ShouldBeValidationDomainErrorsWithIngredientsField()
    {
        // Act
        Error[] errors =
        [
            RecipeErrors.IngredientNameRequired(),
            RecipeErrors.IngredientQuantityNotPositive(),
            RecipeErrors.IngredientUnitWithoutQuantity()
        ];

        // Assert
        errors.Should().HaveCount(3).And.AllSatisfy(error =>
        {
            error.Should().BeOfType<DomainError>()
                .Which.Kind.Should().Be(ErrorKind.Validation);
            error.Metadata.Should().ContainKey("field");
            error.Metadata["field"].Should().Be("ingredients");
        });
    }

    [Fact]
    public void InstructionStepErrors_ShouldBeValidationDomainErrorsWithInstructionsField()
    {
        Error[] errors =
        [
            RecipeErrors.InstructionTextRequired(),
            RecipeErrors.InstructionDurationNotPositive(),
            RecipeErrors.InstructionIngredientNotFound()
        ];

        errors.Should().HaveCount(3).And.AllSatisfy(error =>
        {
            error.Should().BeOfType<DomainError>()
                .Which.Kind.Should().Be(ErrorKind.Validation);
            error.Metadata.Should().ContainKey("field");
            error.Metadata["field"].Should().Be("instructions");
        });
    }
}
