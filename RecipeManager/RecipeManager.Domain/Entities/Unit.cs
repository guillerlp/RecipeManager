namespace RecipeManager.Domain.Entities;

/// <summary>
/// A closed set of units (ADR-022). A null <c>Unit?</c> means "no unit" — there is deliberately no
/// <c>None</c> member, because two ways of expressing nothing is a defect generator.
/// </summary>
public enum Unit
{
    Gram,
    Kilogram,
    Ounce,
    Pound,
    Millilitre,
    Litre,
    Teaspoon,
    Tablespoon,
    Cup,
    FluidOunce,
    Piece,
    Clove,
    Pinch,
    Slice,
    Can,
    Bunch,
    Sprig,
}
