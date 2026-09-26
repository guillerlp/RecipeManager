using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

public sealed class InstructionStep : Entity
{
    private readonly List<Guid> _ingredientIds = [];

    public int Position { get; private set; }
    public string Text { get; private set; }
    public int? DurationMinutes { get; private set; }

    // A fresh read-only copy per access: a caller can neither downcast it to List<Guid> nor mutate the list EF
    // tracks. This is BUG-11's fix applied from the start rather than retrofitted.
    public IReadOnlyList<Guid> IngredientIds => _ingredientIds.ToList().AsReadOnly();

#pragma warning disable CS8618
    private InstructionStep()
    {
        //Constructor needed for EFCore to work properly
    }
#pragma warning restore CS8618

    private InstructionStep(string text, int? durationMinutes, IEnumerable<Guid> ingredientIds)
    {
        Id = Guid.NewGuid();
        Text = text;
        DurationMinutes = durationMinutes;
        _ingredientIds.AddRange(ingredientIds);
    }

    /// <summary>
    /// Always mints the id: nothing references a step yet, so no caller has a legitimate id to supply
    /// (spec 011 §14). Whether each referenced id belongs to the recipe is checked by <see cref="Recipe"/>,
    /// which is the only place that knows the recipe's ingredients.
    /// </summary>
    public static Result<InstructionStep> Create(string text, int? durationMinutes, IEnumerable<Guid>? ingredientIds)
    {
        Result validate = ValidateProperties(text, durationMinutes);

        if (validate.IsFailed)
            return Result.Fail<InstructionStep>(validate.Errors);

        return Result.Ok(new InstructionStep(text, durationMinutes, ingredientIds ?? []));
    }

    // Order belongs to the recipe, not to the step, so only Recipe may set this.
    internal void SetPosition(int position) => Position = position;

    private static Result ValidateProperties(string text, int? durationMinutes)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(text))
            errors.Add(RecipeErrors.InstructionTextRequired());

        if (durationMinutes is <= 0)
            errors.Add(RecipeErrors.InstructionDurationNotPositive());

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(errors);
    }
}
