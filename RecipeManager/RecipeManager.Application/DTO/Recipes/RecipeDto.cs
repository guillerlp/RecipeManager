namespace RecipeManager.Application.DTO.Recipes;

public record RecipeDto(
    Guid Id,
    string Title,
    string Description,
    int PreparationTime,
    int CookingTime,
    int Servings,
    List<IngredientDto> Ingredients,
    List<string> Instructions
);
