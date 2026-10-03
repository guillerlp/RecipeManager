using FluentResults;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.Commands.Recipes;

// Status is nullable so the generated contract marks it optional: absent means Published, today's behaviour.
public record CreateRecipeCommand(
    string Title,
    string? Description,
    int? PreparationTime,
    int? CookingTime,
    int? Servings,
    List<IngredientInputDto> Ingredients,
    List<InstructionStepInputDto> Instructions,
    List<string> Tags,
    RecipeStatus? Status = null) : ICommand<Result<RecipeDto>>;
