namespace RecipeManager.Application.DTO.Recipes;

public record UpdateRecipeDto(
    string Title,
    string Description,
    int PreparationTime,
    int CookingTime,
    int Servings,
    List<IngredientInputDto> Ingredients,
    List<InstructionStepInputDto> Instructions
);
