using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;

namespace RecipeManager.Application.Commands.Cooks;

public record DeleteCookCommand(Guid RecipeId, Guid CookId) : ICommand<Result>;
