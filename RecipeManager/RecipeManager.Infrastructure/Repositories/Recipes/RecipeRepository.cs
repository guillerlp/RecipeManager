using Microsoft.EntityFrameworkCore;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.Infrastructure.Repositories.Recipes;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly AppDbContext _context;

    public RecipeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        await _context.Recipes.AddAsync(recipe, cancellationToken);
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

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Recipe?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Recipes
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        // The recipe must already be tracked (loaded via GetByIdForUpdateAsync): no explicit
        // Update()/Attach() call here, because that would re-mark the whole graph Modified instead
        // of letting EF's change tracker detect the real inserts/updates/deletes. A detached recipe
        // would silently persist nothing -- SaveChangesAsync has no tracked changes to write.
        if (_context.Entry(recipe).State == EntityState.Detached)
        {
            throw new InvalidOperationException(
                $"{nameof(Recipe)} passed to {nameof(UpdateAsync)} is not tracked by this context. " +
                $"Load it via {nameof(GetByIdForUpdateAsync)} before mutating and saving it.");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
