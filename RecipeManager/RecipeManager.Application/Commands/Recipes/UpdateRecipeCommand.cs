using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;

namespace RecipeManager.Application.Commands.Recipes
{
    public record UpdateRecipeCommand(
        Guid Id,
        string Title,
        string Description,
        int PreparationTime,
        int CookingTime,
        int Servings,
        List<string> Ingredients,
        List<string> Instructions
    ) : ICommand<Result>;
}