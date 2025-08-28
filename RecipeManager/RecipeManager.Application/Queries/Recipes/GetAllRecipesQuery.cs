using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Queries.Recipes
{
    public record GetAllRecipesQuery() : IQuery<IEnumerable<RecipeDto>>;
}