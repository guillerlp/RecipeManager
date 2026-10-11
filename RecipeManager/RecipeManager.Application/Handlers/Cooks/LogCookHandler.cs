using FluentResults;
using RecipeManager.Application.Commands.Cooks;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Cooks;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Cooks;

public class LogCookHandler : ICommandHandler<LogCookCommand, Result<CookDto>>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICookRepository _cookRepository;
    private readonly TimeProvider _timeProvider;

    public LogCookHandler(IRecipeRepository recipeRepository, ICookRepository cookRepository,
        TimeProvider timeProvider)
    {
        _recipeRepository = recipeRepository;
        _cookRepository = cookRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CookDto>> Handle(LogCookCommand request, CancellationToken cancellationToken)
    {
        // The cached read is safe here: this is an existence check and the recipe is never mutated, so BUG-14's
        // reason for GetByIdForUpdateAsync does not apply.
        Recipe? recipe = await _recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken);

        if (recipe is null)
            return Result.Fail<CookDto>(RecipeErrors.RecipeNotFound(request.RecipeId));

        Result<Cook> cook = Cook.Create(recipe.Id, request.CookedAt, _timeProvider.GetUtcNow());

        if (cook.IsFailed)
            return Result.Fail<CookDto>(cook.Errors);

        await _cookRepository.AddAsync(cook.Value, cancellationToken);
        return Result.Ok(new CookDto(cook.Value.Id, cook.Value.CookedAt));
    }
}
