using FluentResults;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.Mappings;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Interfaces.Repositories;

namespace RecipeManager.Application.Handlers.Recipes;

public class UpdateRecipeHandler : ICommandHandler<UpdateRecipeCommand, Result>
{
    private readonly IRecipeRepository _recipeRepository;

    public UpdateRecipeHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Result> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        Recipe? recipeToUpdate = await _recipeRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (recipeToUpdate is null)
            return Result.Fail(RecipeErrors.RecipeNotFound(request.Id));

        Result<List<Ingredient>> ingredients = request.Ingredients.ToIngredients();

        if (ingredients.IsFailed)
            return ingredients.ToResult();

        // Steps are resolved against the ingredients just built, so they cannot be mapped until those succeed.
        Result<List<InstructionStep>> instructions = request.Instructions.ToInstructionSteps(ingredients.Value);

        if (instructions.IsFailed)
            return instructions.ToResult();

        Result updateResult = recipeToUpdate.Update(request.Title, request.Description, request.PreparationTime,
            request.CookingTime, request.Servings, ingredients.Value, instructions.Value);

        if (updateResult.IsFailed)
        {
            return updateResult;
        }

        await _recipeRepository.UpdateAsync(recipeToUpdate, cancellationToken);

        return Result.Ok();
    }
}
