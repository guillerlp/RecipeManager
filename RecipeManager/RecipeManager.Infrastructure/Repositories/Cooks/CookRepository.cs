using Microsoft.EntityFrameworkCore;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.Infrastructure.Repositories.Cooks;

// ponytail: no cache decorator. Stats are one indexed GROUP BY per read; add a CachedCookRepository
// (Scrutor Decorate, as CachedRecipeRepository) only if that query shows up in profiling.
public sealed class CookRepository : ICookRepository
{
    private readonly AppDbContext _context;

    public CookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Cook cook, CancellationToken cancellationToken)
    {
        await _context.Cooks.AddAsync(cook, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Cook?> GetByIdAsync(Guid recipeId, Guid cookId, CancellationToken cancellationToken)
    {
        return await _context.Cooks
            .FirstOrDefaultAsync(c => c.Id == cookId && c.RecipeId == recipeId, cancellationToken);
    }

    public async Task DeleteAsync(Cook cook, CancellationToken cancellationToken)
    {
        _context.Cooks.Remove(cook);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CookStats>> GetStatsAsync(IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken)
    {
        if (recipeIds.Count == 0)
            return new Dictionary<Guid, CookStats>();

        // WHERE "RecipeId" = ANY(@ids) GROUP BY "RecipeId" — one round trip for a whole list.
        return await _context.Cooks
            .AsNoTracking()
            .Where(c => recipeIds.Contains(c.RecipeId))
            .GroupBy(c => c.RecipeId)
            .Select(g => new { RecipeId = g.Key, Count = g.Count(), LastCookedAt = g.Max(c => c.CookedAt) })
            .ToDictionaryAsync(s => s.RecipeId, s => new CookStats(s.Count, s.LastCookedAt), cancellationToken);
    }
}
