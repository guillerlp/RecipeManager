using FluentResults;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes;

public class UnpublishRecipeHandler : ICommandHandler<UnpublishRecipeCommand, Result>
{
    private readonly IRecipeRepository _recipeRepository;

    public UnpublishRecipeHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Result> Handle(UnpublishRecipeCommand request, CancellationToken cancellationToken)
    {
        Recipe? recipe = await _recipeRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (recipe is null)
            return Result.Fail(RecipeErrors.RecipeNotFound(request.Id));

        recipe.Unpublish();
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);

        return Result.Ok();
    }
}
