using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.DTO.Recipes;

/// <summary>
/// A null <see cref="Id"/> means "this ingredient is new". On update, the client echoes back the ids it was
/// given so that step references (R-17) survive the edit.
/// </summary>
public record IngredientInputDto(
    Guid? Id,
    decimal? Quantity,
    Unit? Unit,
    string Name,
    string? Notes
);
