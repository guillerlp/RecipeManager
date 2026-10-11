using FluentResults;
using RecipeManager.Application.Commands.Cooks;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Cooks;

public class DeleteCookHandler : ICommandHandler<DeleteCookCommand, Result>
{
    private readonly ICookRepository _cookRepository;

    public DeleteCookHandler(ICookRepository cookRepository)
    {
        _cookRepository = cookRepository;
    }

    public async Task<Result> Handle(DeleteCookCommand request, CancellationToken cancellationToken)
    {
        // One lookup covers a missing recipe too: its cooks cannot exist.
        Cook? cook = await _cookRepository.GetByIdAsync(request.RecipeId, request.CookId, cancellationToken);

        if (cook is null)
            return Result.Fail(CookErrors.CookNotFound(request.CookId));

        await _cookRepository.DeleteAsync(cook, cancellationToken);
        return Result.Ok();
    }
}
