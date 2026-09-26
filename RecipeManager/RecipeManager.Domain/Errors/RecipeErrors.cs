using FluentResults;

namespace RecipeManager.Domain.Errors;

public static class RecipeErrors
{
    public static Error RecipeNotFound(Guid recipeId) =>
        NotFound($"Recipe with ID {recipeId} was not found").Field("id");

    public static Error TitleRequired() =>
        Validation("Title is required").Field("title");

    public static Error DescriptionRequired() =>
        Validation("Description is required").Field("description");

    public static Error PreparationTimeNegative() =>
        Validation("Preparation time cannot be negative").Field("preparationTime");

    public static Error CookingTimeNegative() =>
        Validation("Cooking time cannot be negative").Field("cookingTime");

    public static Error BothTimesZero() =>
        Validation("At least one of preparation or cooking time must be greater than 0")
            .Field("preparationTime,cookingTime");

    public static Error ServingsOutOfRange(int min) =>
        Validation($"Servings must be a least {min}")
            .Field("servings").WithMetadata("min", min);

    public static Error IngredientsRequired() =>
        Validation("At least one ingredient is required").Field("ingredients");

    public static Error IngredientNameRequired() =>
        Validation("Ingredient name is required").Field("ingredients");

    public static Error IngredientQuantityNotPositive() =>
        Validation("Ingredient quantity must be greater than 0 when provided").Field("ingredients");

    public static Error IngredientUnitWithoutQuantity() =>
        Validation("An ingredient unit requires a quantity").Field("ingredients");

    public static Error InstructionsRequired() =>
        Validation("At least one instruction step is required").Field("instructions");

    public static Error InstructionEmpty() =>
        Validation("Instruction steps cannot be empty").Field("instructions");

    public static Error InstructionTextRequired() =>
        Validation("Instruction step text is required").Field("instructions");

    public static Error InstructionDurationNotPositive() =>
        Validation("Instruction step duration must be greater than 0 when provided").Field("instructions");

    public static Error InstructionIngredientNotFound() =>
        Validation("An instruction step references an ingredient that is not in this recipe").Field("instructions");

    private static Error Validation(string message) => new DomainError(message, ErrorKind.Validation);

    private static Error NotFound(string message) => new DomainError(message, ErrorKind.NotFound);

    private static Error Field(this Error error, string field)
        => error.WithMetadata("field", field);
}
