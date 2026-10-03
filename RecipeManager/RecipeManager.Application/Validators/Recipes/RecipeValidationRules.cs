using FluentValidation;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Validators.Recipes;

public static class RecipeValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidateTitle<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Title cannot be null")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");
    }

    // Description is optional (a draft may have none); the cap still applies when it is present.
    public static IRuleBuilderOptions<T, string?> ValidateDescription<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
    }

    // FluentValidation's comparison validators treat a null int? as valid, so these bounds apply only to a
    // value that is present — "required to publish" is a domain rule (spec 013), not shape.
    public static IRuleBuilderOptions<T, int?> ValidatePreparationTime<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage("Preparation time cannot be negative")
            .LessThan(24 * 60).WithMessage("Preparation time cannot exceed 24 hours");
    }

    public static IRuleBuilderOptions<T, int?> ValidateCookingTime<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage("Cooking time cannot be negative")
            .LessThan(24 * 60).WithMessage("Cooking time cannot exceed 24 hours");
    }

    public static IRuleBuilderOptions<T, int?> ValidateServings<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage("Servings must be at least 1")
            .LessThan(1000).WithMessage("Servings cannot exceed 1000");
    }

    // FluentValidation's ForEach() is only declared over IEnumerable<TElement>, so the return type widens
    // to match instead of casting back down to List<IngredientInputDto> — both call sites discard the
    // return value, so nothing downstream needs the narrower type.
    public static IRuleBuilderOptions<T, IEnumerable<IngredientInputDto>> ValidateIngredients<T>(
        this IRuleBuilder<T, List<IngredientInputDto>> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Ingredients list cannot be null")
            .Must(list => list.Count <= 50).WithMessage("Cannot exceed 50 ingredients")
            .ForEach(item => item.SetValidator(new IngredientInputDtoValidator()));
    }

    public static IRuleBuilderOptions<T, IEnumerable<InstructionStepInputDto>> ValidateInstructions<T>(
        this IRuleBuilder<T, List<InstructionStepInputDto>> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Instructions list cannot be null")
            .Must(list => list.Count <= 50).WithMessage("Cannot exceed 50 instruction steps")
            // NotNull per item: a child validator skips null elements, so "instructions": [null] would
            // otherwise reach the handler and fail as a 500 instead of a 400.
            .ForEach(item => item
                .NotNull().WithMessage("Instruction steps cannot be null")
                .SetValidator(new InstructionStepInputDtoValidator()));
    }

    // Limits apply to the raw input: normalising is the domain's rule (spec 014 §6), and a validator that
    // normalised first would duplicate it. The Must is null-safe because rules here run with the default
    // Continue cascade (Cascade() is not reachable from an IRuleBuilder), so a null list still reaches it after
    // failing NotNull; ForEach skips a null collection. Per item, NotNull for the reason ValidateInstructions
    // gives: MVC's implicit-required check covers properties, not list elements.
    public static IRuleBuilderOptions<T, IEnumerable<string>> ValidateTags<T>(
        this IRuleBuilder<T, List<string>> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("Tags list cannot be null")
            .Must(list => list is null || list.Count <= 20).WithMessage("Cannot exceed 20 tags")
            .ForEach(tag => tag
                .NotNull().WithMessage("Tags cannot be null")
                .MaximumLength(40).WithMessage("A tag cannot exceed 40 characters"));
    }
}
