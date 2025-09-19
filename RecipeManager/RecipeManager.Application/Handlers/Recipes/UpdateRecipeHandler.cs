using FluentResults;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class UpdateRecipeHandler : ICommandHandler<UpdateRecipeCommand, Result>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly ILogger<UpdateRecipeHandler> _logger;

        public UpdateRecipeHandler(IRecipeRepository recipeRepository, ILogger<UpdateRecipeHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
        {
            Recipe? recipeToUpdate = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

            if (recipeToUpdate is null)
                return Result.Fail(RecipeErrors.RecipeNotFound(request.Id));

            Result updateResult = recipeToUpdate.Update(request.Title, request.Description, request.PreparationTime,
                request.CookingTime, request.Servings, request.Ingredients, request.Instructions);

            if (updateResult.IsFailed)
            {
                return updateResult;
            }

            await _recipeRepository.UpdateAsync(recipeToUpdate, cancellationToken);

            return Result.Ok();
        }
    }
}