using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        // The recipe is already tracked (loaded via GetByIdForUpdateAsync), so no explicit
        // Update() call: that would re-mark the whole graph -- including owned Ingredients
        // that already have an Id -- as Modified instead of letting EF's change tracker
        // detect the real inserts/updates/deletes.
        //
        // That alone is not enough, though: Ingredient.Id is always set (Ingredient.Create mints a
        // Guid when none is given), so a genuinely new Ingredient discovered only through the
        // replaced collection still LOOKS like an existing one to EF's "is the key already set"
        // heuristic. DetectChanges marks it Modified instead of Added, and SaveChanges then issues
        // an UPDATE against a row that was never there -- 0 rows affected, DbUpdateConcurrencyException.
        // The fix is to tell EF explicitly: an id this recipe did not have before the update is new.
        List<Guid> idsBeforeUpdate = await _context.Recipes
            .AsNoTracking()
            .Where(r => r.Id == recipe.Id)
            .SelectMany(r => r.Ingredients)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        HashSet<Guid> existingIds = idsBeforeUpdate.ToHashSet();
        HashSet<Guid> currentIngredientIds = recipe.Ingredients.Select(i => i.Id).ToHashSet();

        // Scoped to this recipe's own ingredients: the context could in principle be tracking
        // owned Ingredient entries belonging to a different Recipe already loaded in the same scope.
        foreach (EntityEntry<Ingredient> entry in _context.ChangeTracker.Entries<Ingredient>())
        {
            if (entry.State == EntityState.Modified
                && currentIngredientIds.Contains(entry.Entity.Id)
                && !existingIds.Contains(entry.Entity.Id))
                entry.State = EntityState.Added;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
