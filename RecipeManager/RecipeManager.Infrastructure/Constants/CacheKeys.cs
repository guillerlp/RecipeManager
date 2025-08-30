namespace RecipeManager.Infrastructure.Constants;

public static class CacheKeys
{
    public const string AllRecipes = "recipes_all";
    private const string RecipeById = "recipe_{0}";
    public static string GetRecipeKey(Guid id) => string.Format(RecipeById, id);
}