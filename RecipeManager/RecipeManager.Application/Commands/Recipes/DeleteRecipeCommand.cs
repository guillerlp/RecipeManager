using FluentResults;
using MediatR;

namespace RecipeManager.Application.Commands.Recipes
{
    public record class DeleteRecipeCommand ( Guid Id ) : IRequest<Result>;
}
