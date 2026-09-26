namespace RecipeManager.Application.DTO.Recipes;

/// <summary>
/// <see cref="IngredientIndexes"/> points into the <c>ingredients</c> array of the SAME request body, not at
/// ingredient ids: on create the ingredients have no ids yet, so an index is the only reference a client can
/// make (ADR-023). The response carries the resolved ids.
/// </summary>
public record InstructionStepInputDto(
    string Text,
    int? DurationMinutes,
    List<int> IngredientIndexes
);
