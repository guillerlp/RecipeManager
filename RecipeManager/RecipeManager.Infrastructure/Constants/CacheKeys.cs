using RecipeManager.Domain.Entities;

namespace RecipeManager.Infrastructure.Constants;

public static class CacheKeys
{
    private const string RecipesByStatus = "recipes_{0}";
    private const string RecipeById = "recipe_{0}";
    public static string GetRecipesByStatusKey(RecipeStatus status) => string.Format(RecipesByStatus, status);
    public static string GetRecipeKey(Guid id) => string.Format(RecipeById, id);
}
