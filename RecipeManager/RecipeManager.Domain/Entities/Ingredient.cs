using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

public sealed class Ingredient : Entity
{
    public int Position { get; private set; }
    public decimal? Quantity { get; private set; }
    public Unit? Unit { get; private set; }
    public string Name { get; private set; }
    public string? Notes { get; private set; }

#pragma warning disable CS8618
    private Ingredient()
    {
        //Constructor needed for EFCore to work properly
    }
#pragma warning restore CS8618

    private Ingredient(Guid id, decimal? quantity, Unit? unit, string name, string? notes)
    {
        Id = id;
        Quantity = quantity;
        Unit = unit;
        Name = name;
        Notes = notes;
    }

    public static Result<Ingredient> Create(Guid? id, decimal? quantity, Unit? unit, string name, string? notes)
    {
        Result validate = ValidateProperties(quantity, unit, name);

        if (validate.IsFailed)
            return Result.Fail<Ingredient>(validate.Errors);

        return Result.Ok(new Ingredient(id ?? Guid.NewGuid(), quantity, unit, name, notes));
    }

    // Order belongs to the recipe, not to the ingredient, so only Recipe may set this.
    internal void SetPosition(int position) => Position = position;

    private static Result ValidateProperties(decimal? quantity, Unit? unit, string name)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(RecipeErrors.IngredientNameRequired());

        if (quantity is <= 0)
            errors.Add(RecipeErrors.IngredientQuantityNotPositive());

        if (unit is not null && quantity is null)
            errors.Add(RecipeErrors.IngredientUnitWithoutQuantity());

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(errors);
    }
}
