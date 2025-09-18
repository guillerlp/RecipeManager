using FluentResults;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes
{
    public class CreateRecipeHandler : ICommandHandler<CreateRecipeCommand, Result<RecipeDto>>
    {
        private readonly IRecipeRepository _recipeRepository;

        public CreateRecipeHandler(IRecipeRepository recipeRepository)
        {
            _recipeRepository = recipeRepository;
        }

        public async Task<Result<RecipeDto>> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
        {
            Result<Recipe> recipe = Recipe.Create(request.Title, request.Description, request.PreparationTime,
                request.CookingTime, request.Servings, request.Ingredients, request.Instructions);

            if (recipe.IsFailed)
                return Result.Fail<RecipeDto>(recipe.Errors);

            await _recipeRepository.AddAsync(recipe.Value, cancellationToken);
            return Result.Ok(recipe.Value.MapToRecipeDto());
        }
    }
}