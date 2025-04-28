using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class GetRecipeByIdHandler : IRequestHandler<GetRecipeByIdQuery, Result<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetRecipeByIdHandler> _logger;

        public GetRecipeByIdHandler(IRecipeRepository recipeRepository, IMapper mapper, ILogger<GetRecipeByIdHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _mapper = mapper;
            _logger = logger;
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
