using FluentValidation;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Validators.Recipes;

public class UpdateRecipeDtoValidator : AbstractValidator<UpdateRecipeDto>
{
    public UpdateRecipeDtoValidator()
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