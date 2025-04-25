using AutoMapper;
using FluentResults;
using MediatR;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class GetRecipeByIdHandler : IRequestHandler<GetRecipeByIdQuery, Result<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IMapper _mapper;

        public GetRecipeByIdHandler(IRecipeRepository recipeRepository, IMapper mapper)
        {
            _recipeRepository = recipeRepository;
            _mapper = mapper;
        }

        public async Task<Result<RecipeDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);

                var recipeDto = _mapper.Map<RecipeDto>(recipe);

                return Result.Ok(recipeDto);
            }
            catch (KeyNotFoundException knf)
            {
                return Result.Fail<RecipeDto>(knf.Message);
            }
            catch (Exception ex)
            {
                return Result.Fail<RecipeDto>("Error while obtaining recipe by Id").WithError(ex.Message);
            }
        }
    }
}
