using FluentResults;

namespace RecipeManager.Domain.Errors;

public static class CookErrors
{
    public static Error CookNotFound(Guid cookId) =>
        new DomainError($"Cook with ID {cookId} was not found", ErrorKind.NotFound).WithMetadata("field", "id");

    public static Error CookedAtInFuture() =>
        new DomainError("A cook cannot be logged in the future", ErrorKind.Validation)
            .WithMetadata("field", "cookedAt");
}
