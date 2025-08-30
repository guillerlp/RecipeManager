using FluentResults;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
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
            try
            {
                var recipeToUpdate = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

                recipeToUpdate.Update(request.Title, request.Description, request.PreparationTime, request.CookingTime, request.Servings, request.Ingredients, request.Instructions);

                await _recipeRepository.UpdateAsync(recipeToUpdate, cancellationToken);

                return Result.Ok();
            }
            catch (KeyNotFoundException knf)
            {
                _logger.LogWarning(knf, "Tried to update missing recipe {RecipeId}", request.Id);
                return Result.Fail(knf.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe {RecipeId}", request.Id);
                return Result.Fail("Error while updating the recipe").WithError(ex.Message);
            }
        }
    }
}
