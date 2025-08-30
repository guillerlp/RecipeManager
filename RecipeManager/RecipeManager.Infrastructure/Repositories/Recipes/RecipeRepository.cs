using Microsoft.EntityFrameworkCore;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.Infrastructure.Repositories.Recipes
{
    public sealed class RecipeRepository : IRecipeRepository
    {
        private readonly AppDbContext _context;

        public RecipeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
        {
            await _context.Recipes.AddAsync(recipe , cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Recipes.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<Recipe> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var recipe = await _context.Recipes.Where(r => r.Id == id).FirstOrDefaultAsync(cancellationToken);
            if (recipe is null)
                throw new KeyNotFoundException($"Recipe with Id '{id}' not found.");

            return recipe;
        }

        public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken)
        {
            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
