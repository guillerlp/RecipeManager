namespace RecipeManager.Application.DTO.Cooks;

// No validator, deliberately (spec 016 §6): a nullable date has no shape to check, a malformed one is a 400 at model
// binding, and "not in the future" is a business rule in Cook.Create. Null means "just now".
public record LogCookDto(DateTimeOffset? CookedAt);
