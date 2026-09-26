namespace RecipeManager.Application.DTO.Recipes;

public record InstructionStepDto(
    Guid Id,
    string Text,
    int? DurationMinutes,
    List<Guid> IngredientIds
);
