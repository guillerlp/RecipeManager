using FluentValidation;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Validators.Recipes;

public class IngredientInputDtoValidator : AbstractValidator<IngredientInputDto>
{
    public IngredientInputDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotNull().WithMessage("Ingredient name cannot be null")
            .MaximumLength(200).WithMessage("Ingredient name cannot exceed 200 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(200).WithMessage("Ingredient notes cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(0m, 100_000m).WithMessage("Ingredient quantity must be between 0 and 100000")
            .When(x => x.Quantity.HasValue);
    }
}
