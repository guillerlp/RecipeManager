using RecipeManager.Domain.Entities;

namespace RecipeManager.Domain.Interfaces.Repositories
{
    public interface IRecipeRepository
    {
        Task<Recipe> GetByIdAsync(Guid id);
        Task AddAsync(Recipe recipe);
        Task UpdateAsync(Recipe recipe);
        Task DeleteAsync(Guid id);
    }
}
