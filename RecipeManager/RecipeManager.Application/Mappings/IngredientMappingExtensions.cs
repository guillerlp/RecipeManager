using FluentResults;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.Mappings;

public static class IngredientMappingExtensions
{
    /// <summary>
    /// Builds every ingredient, collecting the failures rather than stopping at the first — the same
    /// all-errors-at-once behaviour as <c>Recipe.ValidateProperties</c>.
    /// </summary>
    public static Result<List<Ingredient>> ToIngredients(this IEnumerable<IngredientInputDto>? inputs)
    {
        List<Result<Ingredient>> results = (inputs ?? [])
            .Select(i => Ingredient.Create(i.Id, i.Quantity, i.Unit, i.Name, i.Notes))
            .ToList();

        List<IError> errors = results.Where(r => r.IsFailed).SelectMany(r => r.Errors).ToList();

        return errors.Count > 0
            ? Result.Fail<List<Ingredient>>(errors)
            : Result.Ok(results.Select(r => r.Value).ToList());
    }
}
