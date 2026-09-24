using FluentAssertions;
using FluentResults;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Domain.Entities;

public class IngredientTests
{
    [Fact]
    public void Create_WithAllValues_ShouldReturnSuccessAndKeepThem()
    {
        Result<Ingredient> result = Ingredient.Create(id: null, quantity: 2m, unit: Unit.Tablespoon,
            name: "Butter", notes: "cold");

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().Be(2m);
        result.Value.Unit.Should().Be(Unit.Tablespoon);
        result.Value.Name.Should().Be("Butter");
        result.Value.Notes.Should().Be("cold");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithoutQuantityOrUnit_ShouldReturnSuccess()
    {
        // "salt to taste" is the reason Quantity and Unit are optional at all — and the reason the
        // text[] migration is lossless.
        Result<Ingredient> result = Ingredient.Create(null, null, null, "Salt to taste", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantity.Should().BeNull();
        result.Value.Unit.Should().BeNull();
    }

    [Fact]
    public void Create_WithSuppliedId_ShouldPreserveIt()
    {
        var id = Guid.NewGuid();

        Result<Ingredient> result = Ingredient.Create(id, 1m, Unit.Cup, "Flour", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ShouldFailWithNameRequired(string? name)
    {
        Result<Ingredient> result = Ingredient.Create(null, 1m, Unit.Gram, name!, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.IngredientNameRequired().Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveQuantity_ShouldFail(decimal quantity)
    {
        Result<Ingredient> result = Ingredient.Create(null, quantity, Unit.Gram, "Flour", null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.IngredientQuantityNotPositive().Message);
    }

    [Fact]
    public void Create_WithUnitButNoQuantity_ShouldFail()
    {
        // "tbsp butter" means nothing. A unit is a multiplier with no number to multiply.
        Result<Ingredient> result = Ingredient.Create(null, null, Unit.Tablespoon, "Butter", null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.IngredientUnitWithoutQuantity().Message);
    }

    [Fact]
    public void Create_WithSeveralProblems_ShouldCollectEveryError()
    {
        // Matches Recipe.ValidateProperties' behaviour: collect, never bail on the first.
        Result<Ingredient> result = Ingredient.Create(null, 0m, Unit.Gram, "  ", null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void IngredientErrors_ShouldAllCarryTheIngredientsField()
    {
        // The field drives which input the SPA highlights. All three point at the collection, not at an
        // index, because the API does not expose ingredient positions.
        Error[] errors =
        [
            RecipeErrors.IngredientNameRequired(),
            RecipeErrors.IngredientQuantityNotPositive(),
            RecipeErrors.IngredientUnitWithoutQuantity(),
        ];

        errors.Should().AllSatisfy(e => e.Metadata["field"].Should().Be("ingredients"));
    }
}
