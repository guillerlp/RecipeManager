using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

/// <summary>
/// One time a recipe was cooked. Its own aggregate, referencing <see cref="Recipe"/> by id only (ADR-028): no
/// Recipe invariant needs it, and a recipe's cooks only grow. They die with their recipe through the FK's
/// ON DELETE CASCADE, not through domain code.
/// </summary>
public sealed class Cook : Entity
{
    public Guid RecipeId { get; private set; }
    public DateTimeOffset CookedAt { get; private set; }

    private Cook()
    {
        //Constructor needed for EFCore to work properly
    }

    private Cook(Guid recipeId, DateTimeOffset cookedAt)
    {
        Id = Guid.NewGuid();
        RecipeId = recipeId;
        CookedAt = cookedAt;
    }

    /// <summary>
    /// <paramref name="now"/> is passed in rather than read from a clock, so the rule is deterministic under test.
    /// A null <paramref name="cookedAt"/> means "just now".
    /// </summary>
    public static Result<Cook> Create(Guid recipeId, DateTimeOffset? cookedAt, DateTimeOffset now)
    {
        // Normalised because Npgsql writes timestamptz only from an offset-zero value.
        DateTimeOffset at = (cookedAt ?? now).ToUniversalTime();

        if (at > now)
            return Result.Fail<Cook>(CookErrors.CookedAtInFuture());

        return Result.Ok(new Cook(recipeId, at));
    }
}
