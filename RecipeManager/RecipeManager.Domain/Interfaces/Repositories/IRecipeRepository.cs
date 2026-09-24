using RecipeManager.Domain.Entities;

namespace RecipeManager.Domain.Interfaces.Repositories;

public interface IRecipeRepository
{
    Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken cancellationToken);
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a recipe as a change-tracked entity intended for a subsequent write (update).
    /// This deliberately bypasses the cache, so it always hits the database — callers must
    /// not use it for a plain read, only immediately before mutating and saving the recipe.
    /// </summary>
    Task<Recipe?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);
    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken);
    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken);
}
