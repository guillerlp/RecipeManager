using MediatR;
using RecipeManager.Application.Common;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class GetAllRecipesHandler : IQueryHandler<GetAllRecipesQuery, IEnumerable<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;

        public GetAllRecipesHandler(IRecipeRepository recipeRepository)
        {
            _recipeRepository = recipeRepository;
        }

        public async Task<IEnumerable<RecipeDto>> Handle(GetAllRecipesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _recipeRepository.GetAllAsync(cancellationToken);
            return entities.Select(entity => entity.MapToRecipeDto()).ToList();
        }
    }
}
