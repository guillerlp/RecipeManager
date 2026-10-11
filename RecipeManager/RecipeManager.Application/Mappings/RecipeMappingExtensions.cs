using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Mappings;

public static class RecipeMappingExtensions
{
    // Stats come from the Cook aggregate and are joined here, after the recipe cache (ADR-028): the cached entity
    // never holds them, so logging a cook has nothing to invalidate. No stats means never cooked.
    public static RecipeDto MapToRecipeDto(this Recipe recipe, CookStats? stats = null)
    {
        return new RecipeDto(
            recipe.Id,
            recipe.Title,
            recipe.Status,
            recipe.Description,
            recipe.PreparationTime,
            recipe.CookingTime,
            recipe.Servings,
            recipe.Ingredients.Select(i => i.MapToIngredientDto()).ToList(),
            recipe.Instructions.Select(s => s.MapToInstructionStepDto()).ToList(),
            recipe.Tags.ToList(),
            stats?.Count ?? 0,
            stats?.LastCookedAt);
    }

    public static InstructionStepDto MapToInstructionStepDto(this InstructionStep step)
    {
        return new InstructionStepDto(
            step.Id,
            step.Text,
            step.DurationMinutes,
            step.IngredientIds.ToList());
    }

    public static IngredientDto MapToIngredientDto(this Ingredient ingredient)
    {
        return new IngredientDto(
            ingredient.Id,
            ingredient.Quantity,
            ingredient.Unit,
            ingredient.Name,
            ingredient.Notes);
    }
}
