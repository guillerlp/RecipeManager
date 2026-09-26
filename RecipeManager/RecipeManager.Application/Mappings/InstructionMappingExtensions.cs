using FluentResults;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.Application.Mappings;

public static class InstructionMappingExtensions
{
    /// <summary>
    /// Translates the wire's local references (indexes into this payload's ingredients) into the domain's
    /// global ones (ingredient ids), then builds each step — collecting every failure, like
    /// <see cref="IngredientMappingExtensions.ToIngredients"/>. It works because <c>Ingredient.Create</c> has
    /// already minted each id. An index that cannot produce an id fails here with the same error the domain
    /// raises for an unknown id, so the client sees one error for one concept (spec 011 §7).
    /// </summary>
    public static Result<List<InstructionStep>> ToInstructionSteps(
        this IEnumerable<InstructionStepInputDto>? inputs, IReadOnlyList<Ingredient> ingredients)
    {
        var errors = new List<IError>();
        var steps = new List<InstructionStep>();

        foreach (InstructionStepInputDto input in inputs ?? [])
        {
            List<int> indexes = input.IngredientIndexes ?? [];
            bool IsInRange(int i) => i >= 0 && i < ingredients.Count;

            if (!indexes.All(IsInRange))
                errors.Add(RecipeErrors.InstructionIngredientNotFound());

            // Build the step even when an index was bad, so its own text/duration errors are reported in the
            // same response rather than on the client's next attempt.
            Result<InstructionStep> step = InstructionStep.Create(input.Text, input.DurationMinutes,
                indexes.Where(IsInRange).Select(i => ingredients[i].Id));

            if (step.IsFailed)
                errors.AddRange(step.Errors);
            else
                steps.Add(step.Value);
        }

        return errors.Count > 0
            ? Result.Fail<List<InstructionStep>>(errors)
            : Result.Ok(steps);
    }
}
