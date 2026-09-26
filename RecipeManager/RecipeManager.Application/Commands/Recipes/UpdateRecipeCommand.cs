using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Commands.Recipes;

public record UpdateRecipeCommand(
    Guid Id,
    string Title,
    string Description,
    int PreparationTime,
    int CookingTime,
    int Servings,
    List<IngredientInputDto> Ingredients,
    List<InstructionStepInputDto> Instructions
) : ICommand<Result>;
