using Microsoft.Extensions.Logging;
using RecipeManager.Application.Common.Interfaces.Caching;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Constants;

namespace RecipeManager.Infrastructure.Repositories.Recipes;

public sealed class CachedRecipeRepository : IRecipeRepository
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedRecipeRepository> _logger;

    public CachedRecipeRepository(IRecipeRepository recipeRepository, ICacheService cacheService,
        ILogger<CachedRecipeRepository> logger)
    {
        _recipeRepository = recipeRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IEnumerable<Recipe>> GetAllAsync(RecipeStatus status, CancellationToken cancellationToken)
    {
        string cacheKey = CacheKeys.GetRecipesByStatusKey(status);
        IEnumerable<Recipe>? cachedRecipes =
            await _cacheService.GetAsync<IEnumerable<Recipe>>(cacheKey, cancellationToken);

        if (cachedRecipes is not null)
            return cachedRecipes;

        List<Recipe> recipes = (await _recipeRepository.GetAllAsync(status, cancellationToken)).ToList();

        await _cacheService.SetAsync(cacheKey, recipes, CacheDuration.DefaultExpiration,
            CacheDuration.DefaultSliding, cancellationToken);

        return recipes;
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        string cacheKey = CacheKeys.GetRecipeKey(id);
        Recipe? cachedRecipe = await _cacheService.GetAsync<Recipe>(cacheKey, cancellationToken);

        if (cachedRecipe is not null)
            return cachedRecipe;

        Recipe? recipe = await _recipeRepository.GetByIdAsync(id, cancellationToken);

        if (recipe is not null)
        {
            await SetCache(cacheKey, recipe, CacheDuration.DefaultExpiration,
                CacheDuration.DefaultSliding, cancellationToken);
        }

        return recipe;
    }

    // Deliberately no cache read or write: a cached Recipe instance is detached and shared
    // across requests, so handing it out for mutation would let one request's changes leak
    // into another's cached copy. Writes always go straight to the database.
    public Task<Recipe?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return _recipeRepository.GetByIdForUpdateAsync(id, cancellationToken);
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        await _recipeRepository.AddAsync(recipe, cancellationToken);
        await InvalidateRecipeLists(cancellationToken);
        await SetCache(CacheKeys.GetRecipeKey(recipe.Id), recipe, CacheDuration.LongExpiration,
            CacheDuration.LongSliding, cancellationToken);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        await InvalidateRecipeRelatedCaches(recipe.Id, cancellationToken);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        await _recipeRepository.DeleteAsync(recipe, cancellationToken);
        await InvalidateRecipeRelatedCaches(recipe.Id, cancellationToken);
    }

    private async Task SetCache<T>(string cacheKey, T value, TimeSpan expiration, TimeSpan sliding,
        CancellationToken cancellationToken)
    {
        try
        {
            await _cacheService.SetAsync(cacheKey, value, expiration, sliding, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache for {Key}", cacheKey);
        }
    }

    private async Task RemoveCache(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cacheService.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove {Key}", key);
        }
    }

    // Every list, not just the one the recipe is in: a write does not know the recipe's previous status
    // without loading it again, and clearing two keys is trivially correct (spec 013 §9).
    private Task InvalidateRecipeLists(CancellationToken cancellationToken) =>
        Task.WhenAll(Enum.GetValues<RecipeStatus>()
            .Select(status => RemoveCache(CacheKeys.GetRecipesByStatusKey(status), cancellationToken)));

    private Task InvalidateRecipeRelatedCaches(Guid recipeId, CancellationToken cancellationToken) =>
        Task.WhenAll(
            InvalidateRecipeLists(cancellationToken),
            RemoveCache(CacheKeys.GetRecipeKey(recipeId), cancellationToken));
}
