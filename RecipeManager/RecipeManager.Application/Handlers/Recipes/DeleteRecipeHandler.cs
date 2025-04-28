using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class DeleteRecipeHandler : IRequestHandler<DeleteRecipeCommand, Result>
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
                var recipeToDelete = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

                await _recipeRepository.DeleteAsync(recipeToDelete, cancellationToken);

                return Result.Ok();
            }
            catch (KeyNotFoundException knf)
            {
                _logger.LogWarning(knf, "Tried to delete missing recipe {RecipeId}", request.Id);
                return Result.Fail(knf.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId}", request.Id);
                return Result.Fail("Error while deleting the recipe").WithError(ex.Message);
            }
        }
    }
}
