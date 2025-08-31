using FluentValidation;
using RecipeManager.Application.Commands.Recipes;

namespace RecipeManager.Application.Validators.Recipes
{
    public class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
    {
        public CreateRecipeCommandValidator()
        {
            RuleFor(x => x.Title).ValidateTitle();
            RuleFor(x => x.Description).ValidateDescription();
            RuleFor(x => x.PreparationTime).ValidatePreparationTime();
            RuleFor(x => x.CookingTime).ValidateCookingTime();
            RuleFor(x => x.Servings).ValidateServings();
            RuleFor(x => x.Ingredients).ValidateIngredients();
            RuleFor(x => x.Instructions).ValidateInstructions();
        }
    }
}
