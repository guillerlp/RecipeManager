using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes;

public class GetAllRecipesHandler : IQueryHandler<GetAllRecipesQuery, IEnumerable<RecipeDto>>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICookRepository _cookRepository;

    public GetAllRecipesHandler(IRecipeRepository recipeRepository, ICookRepository cookRepository)
    {
        _recipeRepository = recipeRepository;
        _cookRepository = cookRepository;
    }

    public async Task<IEnumerable<RecipeDto>> Handle(GetAllRecipesQuery request, CancellationToken cancellationToken)
    {
        List<Recipe> entities = (await _recipeRepository.GetAllAsync(request.Status, cancellationToken)).ToList();
        IReadOnlyDictionary<Guid, CookStats> stats =
            await _cookRepository.GetStatsAsync(entities.Select(r => r.Id).ToList(), cancellationToken);

        return entities.Select(entity => entity.MapToRecipeDto(stats.GetValueOrDefault(entity.Id))).ToList();
    }
}
