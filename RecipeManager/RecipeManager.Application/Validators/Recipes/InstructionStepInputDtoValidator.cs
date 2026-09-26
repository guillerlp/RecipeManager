using FluentValidation;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Validators.Recipes;

public class InstructionStepInputDtoValidator : AbstractValidator<InstructionStepInputDto>
{
    public InstructionStepInputDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotNull().WithMessage("Instruction text cannot be null")
            .MaximumLength(2000).WithMessage("Instruction text cannot exceed 2000 characters");

        // Lower bound is >= 0, not > 0: "a duration is positive" is a business rule the domain owns
        // (InstructionDurationNotPositive, 422). Same split as Ingredient.Quantity.
        RuleFor(x => x.DurationMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Instruction duration cannot be negative")
            .LessThan(24 * 60).WithMessage("Instruction duration cannot exceed 24 hours")
            .When(x => x.DurationMinutes.HasValue);

        // Stop after NotNull: the Must predicates below would otherwise run on a null list and throw.
        RuleFor(x => x.IngredientIndexes)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Ingredient indexes cannot be null")
            .Must(list => list.Count <= 50).WithMessage("A step cannot reference more than 50 ingredients")
            .Must(list => list.All(i => i >= 0)).WithMessage("Ingredient indexes cannot be negative")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("A step cannot reference the same ingredient twice");
    }
}
