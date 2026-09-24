using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.DTO.Recipes;

public record IngredientDto(
    Guid Id,
    decimal? Quantity,
    Unit? Unit,
    string Name,
    string? Notes
);
