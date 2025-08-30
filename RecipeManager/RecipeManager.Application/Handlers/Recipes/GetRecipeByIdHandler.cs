using FluentResults;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class GetRecipeByIdHandler : IQueryHandler<GetRecipeByIdQuery, Result<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly ILogger<GetRecipeByIdHandler> _logger;

        public GetRecipeByIdHandler(IRecipeRepository recipeRepository, ILogger<GetRecipeByIdHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _logger = logger;
        }

        public async Task<Result<RecipeDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

                var recipeDto = recipe.MapToRecipeDto();

                return Result.Ok(recipeDto);
            }
            catch (KeyNotFoundException knf)
            {
                _logger.LogWarning(knf, "Tried to obtain missing recipe {RecipeId}", request.Id);
                return Result.Fail<RecipeDto>(knf.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining recipe {RecipeId}", request.Id);
                return Result.Fail<RecipeDto>("Error while obtaining recipe by Id").WithError(ex.Message);
            }
        }
    }
}
