using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.DTO.Recipes;

public record RecipeDto(
    Guid Id,
    string Title,
    RecipeStatus Status,
    string? Description,
    int? PreparationTime,
    int? CookingTime,
    int? Servings,
    List<IngredientDto> Ingredients,
    List<InstructionStepDto> Instructions
);
