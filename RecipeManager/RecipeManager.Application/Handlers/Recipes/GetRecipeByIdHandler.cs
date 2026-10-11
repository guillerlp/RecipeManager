using FluentResults;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes;

public class GetRecipeByIdHandler : IQueryHandler<GetRecipeByIdQuery, Result<RecipeDto>>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICookRepository _cookRepository;
    private readonly ILogger<GetRecipeByIdHandler> _logger;

    public GetRecipeByIdHandler(IRecipeRepository recipeRepository, ICookRepository cookRepository,
        ILogger<GetRecipeByIdHandler> logger)
    {
        _recipeRepository = recipeRepository;
        _cookRepository = cookRepository;
        _logger = logger;
    }

    public async Task<Result<RecipeDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

        if (recipe is null)
            return Result.Fail<RecipeDto>(RecipeErrors.RecipeNotFound(request.Id));

        IReadOnlyDictionary<Guid, CookStats> stats =
            await _cookRepository.GetStatsAsync([recipe.Id], cancellationToken);

        return Result.Ok(recipe.MapToRecipeDto(stats.GetValueOrDefault(recipe.Id)));
    }
}
