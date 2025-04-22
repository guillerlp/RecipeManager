using Microsoft.EntityFrameworkCore;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.Infrastructure.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        #region Fields
        private readonly AppDbContext _context;
        #endregion

        #region Constructor
        public RecipeRepository(AppDbContext context)
        {
            _context = context;
        }
        #endregion

        public async Task AddAsync(Recipe recipe)
        {
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var recipeToDelete = GetRecipeByIdAsync(id);

            if (recipeToDelete is null)
            {
                throw new ArgumentException();
            }

            _context.Remove(recipeToDelete);
            await _context.SaveChangesAsync();
        }

        public async Task<Recipe> GetByIdAsync(Guid id)
        {
            var recipe = await GetRecipeByIdAsync(id);

            if (recipe == null)
            {
                throw new ArgumentException();
            }

            return recipe;
        }

        public async Task UpdateAsync(Recipe recipe)
        {
            var recipeToUpdate = GetRecipeByIdAsync(recipe.Id);

            if (recipeToUpdate is null)
            {
                throw new ArgumentException();
            }

            _context.Update(recipeToUpdate);
            await _context.SaveChangesAsync();
        }

        #region Private methods 

        private async Task<Recipe?> GetRecipeByIdAsync(Guid id)
        {
            return await _context.Recipes.FirstOrDefaultAsync(recipe => recipe.Id == id);
        }

        #endregion
    }
}
