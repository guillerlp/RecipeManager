using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Cooks;

namespace RecipeManager.Application.Commands.Cooks;

public record LogCookCommand(Guid RecipeId, DateTimeOffset? CookedAt) : ICommand<Result<CookDto>>;
