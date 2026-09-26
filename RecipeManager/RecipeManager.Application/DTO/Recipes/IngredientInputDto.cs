using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.DTO.Recipes;

/// <summary>
/// A null <see cref="Id"/> means "this ingredient is new". On update, the client echoes back the ids it was
/// given so an ingredient keeps its identity across the edit. Step references do not depend on it: they are
/// sent as indexes and re-resolved on every write (ADR-023).
/// </summary>
public record IngredientInputDto(
    Guid? Id,
    decimal? Quantity,
    Unit? Unit,
    string Name,
    string? Notes
);
