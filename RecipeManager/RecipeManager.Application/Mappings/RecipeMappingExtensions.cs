using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.Mappings;

public static class RecipeMappingExtensions
{
    public static RecipeDto MapToRecipeDto(this Recipe recipe)
    {
        return new RecipeDto(
            recipe.Id,
            recipe.Title,
            recipe.Description,
            recipe.PreparationTime,
            recipe.CookingTime,
            recipe.Servings,
            recipe.Ingredients.Select(i => i.MapToIngredientDto()).ToList(),
            recipe.Instructions.ToList());
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
