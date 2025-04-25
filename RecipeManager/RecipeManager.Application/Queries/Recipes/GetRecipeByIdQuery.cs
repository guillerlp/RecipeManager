using FluentResults;
using MediatR;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Queries.Recipes
{
    public record GetRecipeByIdQuery(Guid Id): IRequest<Result<RecipeDto>>;
}
