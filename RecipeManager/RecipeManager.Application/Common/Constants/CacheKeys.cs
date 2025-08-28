namespace RecipeManager.Application.Common.Constants;

public static class CacheKeys
{
    public const string ALL_RECIPES = "recipes_all";
    private const string RECIPE_BY_ID = "recipe_{0}";
    public const string RECIPES_PATTERN = "recipe_*";

    public static string GetRecipeKey(Guid id) => string.Format(RECIPE_BY_ID, id);
}