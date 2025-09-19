using FluentValidation;

namespace RecipeManager.Application.Validators.Recipes;

public static class RecipeValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidateTitle<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Title cannot be null")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");
    }

    public static IRuleBuilderOptions<T, string> ValidateDescription<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Description cannot be null")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
    }

    public static IRuleBuilderOptions<T, int> ValidatePreparationTime<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage("Preparation time cannot be negative")
            .LessThan(24 * 60).WithMessage("Preparation time cannot exceed 24 hours"); 
    }

    public static IRuleBuilderOptions<T, int> ValidateCookingTime<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage("Cooking time cannot be negative")
            .LessThan(24 * 60).WithMessage("Cooking time cannot exceed 24 hours");
    }

    public static IRuleBuilderOptions<T, int> ValidateServings<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage("Servings must be at least 1")
            .LessThan(1000).WithMessage("Servings cannot exceed 1000");
    }

    public static IRuleBuilderOptions<T, List<string>> ValidateIngredients<T>(
        this IRuleBuilder<T, List<string>> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Ingredients list cannot be null")
            .Must(list => list.Count <= 50).WithMessage("Cannot exceed 50 ingredients"); 
    }

    public static IRuleBuilderOptions<T, List<string>> ValidateInstructions<T>(
        this IRuleBuilder<T, List<string>> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Instructions list cannot be null")
            .Must(list => list.Count <= 50).WithMessage("Cannot exceed 50 instruction steps");
    }
}