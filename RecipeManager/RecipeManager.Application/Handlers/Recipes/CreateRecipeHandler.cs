using FluentResults;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes;

public class CreateRecipeHandler : ICommandHandler<CreateRecipeCommand, Result<RecipeDto>>
{
    private readonly IRecipeRepository _recipeRepository;

    public CreateRecipeHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Result<RecipeDto>> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        Result<List<Ingredient>> ingredients = request.Ingredients.ToIngredients();

        if (ingredients.IsFailed)
            return Result.Fail<RecipeDto>(ingredients.Errors);

        Result<Recipe> recipe = Recipe.Create(request.Title, request.Description, request.PreparationTime,
            request.CookingTime, request.Servings, ingredients.Value, request.Instructions);

        if (recipe.IsFailed)
            return Result.Fail<RecipeDto>(recipe.Errors);

        await _recipeRepository.AddAsync(recipe.Value, cancellationToken);
        return Result.Ok(recipe.Value.MapToRecipeDto());
    }
}
