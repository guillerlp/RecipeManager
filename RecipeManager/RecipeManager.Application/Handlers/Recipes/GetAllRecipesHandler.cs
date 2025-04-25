using AutoMapper;
using MediatR;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class GetAllRecipesHandler : IRequestHandler<GetAllRecipesQuery, IEnumerable<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IMapper _mapper;

        public GetAllRecipesHandler(IRecipeRepository recipeRepository, IMapper mapper)
        {
            _recipeRepository = recipeRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RecipeDto>> Handle(GetAllRecipesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _recipeRepository.GetAllAsync(cancellationToken);
            return entities.Select(entity => _mapper.Map<RecipeDto>(entity)).ToList();
        }
    }
}
