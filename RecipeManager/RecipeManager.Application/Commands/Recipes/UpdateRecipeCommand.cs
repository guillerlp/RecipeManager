using FluentResults;
using MediatR;

namespace RecipeManager.Application.Commands.Recipes
{
    public record class UpdateRecipeCommand (Guid Id, string Title, string Description, int PreparationTime, int CookingTime, int Servings, List<string> Ingredients, List<string> Instructions) : IRequest<Result>;
}
