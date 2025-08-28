using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;

namespace RecipeManager.Application.Commands.Recipes
{
    public record DeleteRecipeCommand(Guid Id) : ICommand<Result>;
}