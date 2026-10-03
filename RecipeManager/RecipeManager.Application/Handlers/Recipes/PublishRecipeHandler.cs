using FluentResults;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes;

public class PublishRecipeHandler : ICommandHandler<PublishRecipeCommand, Result>
{
    private readonly IRecipeRepository _recipeRepository;

    public PublishRecipeHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Result> Handle(PublishRecipeCommand request, CancellationToken cancellationToken)
    {
        Recipe? recipe = await _recipeRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (recipe is null)
            return Result.Fail(RecipeErrors.RecipeNotFound(request.Id));

        Result publishResult = recipe.Publish();

        if (publishResult.IsFailed)
            return publishResult;

        // Through UpdateAsync, so the cache decorator's existing invalidation covers the move between lists.
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);

        return Result.Ok();
    }
}
