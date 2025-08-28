using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Commands.Recipes
{
    public record CreateRecipeCommand(
        string Title,
        string Description,
        int PreparationTime,
        int CookingTime,
        int Servings,
        List<string> Ingredients,
        List<string> Instructions) : ICommand<Result<RecipeDto>>;
}