using FluentValidation;

namespace RecipeManager.Application.Validators.Recipes;

public static class RecipeValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidateTitle<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().WithMessage("Title is required.");
    }

    public static IRuleBuilderOptions<T, string> ValidateDescription<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().WithMessage("Description is required.");
    }

    public static IRuleBuilderOptions<T, int> ValidatePreparationTime<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage("{PropertyName} must be zero or a positive number.")
            .WithName("Preparation time");
    }

    public static IRuleBuilderOptions<T, int> ValidateCookingTime<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage("{PropertyName} must be zero or a positive number.")
            .WithName("Cooking time");
    }

    public static IRuleBuilderOptions<T, int> ValidateServings<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage("{PropertyName} must be at least {ComparisonValue}.")
            .WithName("Servings");
    }

    public static IRuleBuilderOptions<T, List<string>> ValidateIngredients<T>(
        this IRuleBuilder<T, List<string>> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("At least one ingredient is necessary.");
    }

    public static IRuleBuilderOptions<T, List<string>> ValidateInstructions<T>(
        this IRuleBuilder<T, List<string>> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("At least one instruction step is necessary.");
    }
}