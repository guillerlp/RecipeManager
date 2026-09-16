using FluentResults;

namespace RecipeManager.Domain.Errors;

public static class RecipeErrors
{
    public static Error RecipeNotFound(Guid recipeId) =>
        NotFound($"Recipe with ID {recipeId} was not found").WithCode(404).Field("id");

    public static Error TitleRequired() =>
        Validation("Title is required").WithCode(422).Field("title");

    public static Error DescriptionRequired() =>
        Validation("Description is required").WithCode(422).Field("description");

    public static Error PreparationTimeNegative() =>
        Validation("Preparation time cannot be negative").WithCode(422).Field("preparationTime");

    public static Error CookingTimeNegative() =>
        Validation("Cooking time cannot be negative").WithCode(422).Field("cookingTime");

    public static Error BothTimesZero() =>
        Validation("At least one of preparation or cooking time must be greater than 0")
            .WithCode(422).Field("preparationTime,cookingTime");

    public static Error ServingsOutOfRange(int min) =>
        Validation($"Servings must be a least {min}")
            .WithCode(422).Field("servings").WithMetadata("min", min);

    public static Error IngredientsRequired() =>
        Validation("At least one ingredient is required").WithCode(422).Field("ingredients");

    public static Error IngredientEmpty() =>
        Validation("Ingredients cannot be empty").WithCode(422).Field("ingredients");

    public static Error InstructionsRequired() =>
        Validation("At least one instruction step is required").WithCode(422).Field("instructions");

    public static Error InstructionEmpty() =>
        Validation("Instruction steps cannot be empty").WithCode(422).Field("instructions");

    private static Error Validation(string message) => new DomainError(message, ErrorKind.Validation);

    private static Error NotFound(string message) => new DomainError(message, ErrorKind.NotFound);

    private static Error WithCode(this Error error, int code)
        => error.WithMetadata("ErrorCode", code);

    private static Error Field(this Error error, string field)
        => error.WithMetadata("field", field);
}