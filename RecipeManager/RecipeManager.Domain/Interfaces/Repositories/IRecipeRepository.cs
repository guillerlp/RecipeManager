using RecipeManager.Domain.Entities;

namespace RecipeManager.Domain.Interfaces.Repositories
{
    public interface IRecipeRepository
    {
        Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken cancellationToken);
        Task<Recipe> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(Recipe recipe, CancellationToken cancellationToken);
        Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken);
        Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken);
    }
}
