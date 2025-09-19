using FluentResults;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Errors;
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
            var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

            if (recipe is null)
                return Result.Fail<RecipeDto>(RecipeErrors.RecipeNotFound(request.Id));

            return Result.Ok(recipe.MapToRecipeDto());
        }
    }
}
