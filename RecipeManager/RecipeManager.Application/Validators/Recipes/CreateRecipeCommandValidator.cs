using FluentValidation;
using RecipeManager.Application.Commands.Recipes;

namespace RecipeManager.Application.Validators.Recipes
{
    public class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
    {
        public CreateRecipeCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required.");

            RuleFor(x => x.PreparationTime)
                .GreaterThanOrEqualTo(0)
                .WithMessage("{PropertyName} must be zero or a positive number.")
                .WithName("Preparation time"); ;

            RuleFor(x => x.CookingTime)
                .GreaterThanOrEqualTo(0)
                .WithMessage("{PropertyName} must be zero or a positive number.")
                .WithName("Cooking time");

            RuleFor(x => x.Servings)
                .GreaterThan(0)
                .WithMessage("{PropertyName} must be at least {ComparisonValue}.")
                .WithName("Servings");

            RuleFor(x => x.Ingredients)
                .NotEmpty()
                .WithMessage("At least one ingredient is necessary.");

            RuleFor(x => x.Instructions)
                .NotEmpty()
                .WithMessage("At least one instruction step is necessary.");
        }
    }
}
