using RecipeManager.Domain.Entities;

namespace RecipeManager.Domain.Interfaces.Repositories;

/// <summary>A recipe's cook count and latest cook. Absent from <see cref="ICookRepository.GetStatsAsync"/> when never cooked.</summary>
public sealed record CookStats(int Count, DateTimeOffset LastCookedAt);

public interface ICookRepository
{
    Task AddAsync(Cook cook, CancellationToken cancellationToken);

    /// <summary>
    /// Scoped to the recipe: a cook of another recipe is not found, so a route naming the wrong recipe cannot
    /// delete it (the BUG-15 class of bug, not repeated here). Tracked, because its only caller deletes it.
    /// </summary>
    Task<Cook?> GetByIdAsync(Guid recipeId, Guid cookId, CancellationToken cancellationToken);

    Task DeleteAsync(Cook cook, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, CookStats>> GetStatsAsync(IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken);
}
