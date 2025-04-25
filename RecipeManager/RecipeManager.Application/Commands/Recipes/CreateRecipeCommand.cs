using FluentResults;
using MediatR;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Commands.Recipes
{
    public record CreateRecipeCommand(string Title, string Description, int PreparationTime, int CookingTime, int Servings, List<string> Ingredients, List<string> Instructions) : IRequest<Result<RecipeDto>>;
}
