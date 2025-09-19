using FluentResults;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class DeleteRecipeHandler : ICommandHandler<DeleteRecipeCommand, Result>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly ILogger<DeleteRecipeHandler> _logger;

        public DeleteRecipeHandler(IRecipeRepository recipeRepository, ILogger<DeleteRecipeHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Recipe? recipeToDelete = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);
                
                if(recipeToDelete is null)
                    return Result.Fail(RecipeErrors.RecipeNotFound(request.Id));
                
                await _recipeRepository.DeleteAsync(recipeToDelete, cancellationToken);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId}", request.Id);
                return Result.Fail("Error while deleting the recipe").WithError(ex.Message);
            }
        }
    }
}
