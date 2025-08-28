using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Queries.Recipes
{
    public record GetRecipeByIdQuery(Guid Id) : IQuery<Result<RecipeDto>>;
}