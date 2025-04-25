using AutoMapper;
using FluentResults;
using MediatR;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class CreateRecipeHandler : IRequestHandler<CreateRecipeCommand, Result<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IMapper _mapper;

        public CreateRecipeHandler(IRecipeRepository recipeRepository, IMapper mapper)
        {
            _recipeRepository = recipeRepository;
            _mapper = mapper;
        }

        public async Task<Result<RecipeDto>> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
        {
            var recipe = Recipe.Create(request.Title, request.Description, request.PreparationTime, request.CookingTime, request.Servings, request.Ingredients, request.Instructions);

            await _recipeRepository.AddAsync(recipe, cancellationToken);

            var recipeDto = _mapper.Map<RecipeDto>(recipe);

            return Result.Ok(recipeDto);
        }
    }
}
