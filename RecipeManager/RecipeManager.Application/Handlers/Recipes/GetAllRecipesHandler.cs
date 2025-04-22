using AutoMapper;
using MediatR;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class GetAllRecipesHandler : IRequestHandler<GetAllRecipesQuery, IEnumerable<RecipeDto>>
    {
        #region Fields
        private readonly IRecipeRepository _recipeRepository;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public GetAllRecipesHandler(IRecipeRepository recipeRepository, IMapper mapper)
        {
            _recipeRepository = recipeRepository;
            _mapper = mapper;
        }
        #endregion

        public async Task<IEnumerable<RecipeDto>> Handle(GetAllRecipesQuery request, CancellationToken cancellationToken)
        {
            var result = await _recipeRepository.GetAllAsync(cancellationToken);
            return result.Select(entity => _mapper.Map<RecipeDto>(entity));
        }
    }
}
