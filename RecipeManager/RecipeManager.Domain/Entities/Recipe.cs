using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

public sealed class Recipe : Entity
{
    private readonly List<Ingredient> _ingredients = [];
    private readonly List<InstructionStep> _instructions = [];
    private readonly List<string> _tags = [];

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public int? PreparationTime { get; private set; }
    public int? CookingTime { get; private set; }
    public int? Servings { get; private set; }
    public RecipeStatus Status { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients => _ingredients.OrderBy(i => i.Position).ToList().AsReadOnly();
    public IReadOnlyList<InstructionStep> Instructions =>
        _instructions.OrderBy(s => s.Position).ToList().AsReadOnly();
    public IReadOnlyList<string> Tags => _tags.ToList().AsReadOnly();

#pragma warning disable CS8618
    private Recipe()
    {
        //Constructor needed for EFCore to work properly
    }
#pragma warning restore CS8618

    private Recipe(string title, string? description, int? preparationTime, int? cookingTime, int? servings,
        List<Ingredient> ingredients, List<InstructionStep> instructions, List<string> tags, RecipeStatus status)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
        Servings = servings;
        Status = status;
        ReplaceIngredients(ingredients);
        ReplaceInstructions(instructions);
        _tags.AddRange(tags);
    }

    // Tags come last and default to none so existing callers keep compiling: a new recipe with no tags is the
    // truthful default. Update takes them as a required argument, where omitting them would silently clear them.
    public static Result<Recipe> Create(string title, string? description, int? preparationTime, int? cookingTime,
        int? servings, IEnumerable<Ingredient> ingredients, IEnumerable<InstructionStep> instructions,
        RecipeStatus status = RecipeStatus.Published, IEnumerable<string>? tags = null)
    {
        // Materialise once: each list is read by ValidateProperties and again by the constructor, and an
        // IEnumerable is not guaranteed to survive being walked twice.
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];
        List<InstructionStep> instructionList = instructions?.ToList() ?? [];
        List<string> tagList = NormaliseTags(tags);

        Result validate = ValidateProperties(status, title, description, preparationTime, cookingTime, servings,
            ingredientList, instructionList, tagList);

        if (validate.IsFailed)
            return Result.Fail<Recipe>(validate.Errors);

        return Result.Ok(new Recipe(title, description, preparationTime, cookingTime, servings, ingredientList,
            instructionList, tagList, status));
    }

    // Validates against the current status: a published recipe keeps the full rules on every edit.
    public Result Update(string title, string? description, int? preparationTime, int? cookingTime,
        int? servings, IEnumerable<Ingredient> ingredients, IEnumerable<InstructionStep> instructions,
        IEnumerable<string> tags)
    {
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];
        List<InstructionStep> instructionList = instructions?.ToList() ?? [];
        List<string> tagList = NormaliseTags(tags);

        Result validate = ValidateProperties(Status, title, description, preparationTime, cookingTime, servings,
            ingredientList, instructionList, tagList);

        if (validate.IsFailed)
            return validate;

        Title = title;
        Description = description;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
        Servings = servings;
        ReplaceIngredients(ingredientList);
        ReplaceInstructions(instructionList);
        _tags.Clear();
        _tags.AddRange(tagList);

        return Result.Ok();
    }

    // Idempotent: an already-published recipe met these rules on every write that got it here.
    public Result Publish()
    {
        if (Status == RecipeStatus.Published)
            return Result.Ok();

        Result validate = ValidateProperties(RecipeStatus.Published, Title, Description, PreparationTime,
            CookingTime, Servings, _ingredients, _instructions, _tags);

        if (validate.IsFailed)
            return validate;

        Status = RecipeStatus.Published;
        return Result.Ok();
    }

    // Cannot fail: the draft rules are a strict subset of the published ones, so there is nothing to report.
    public void Unpublish() => Status = RecipeStatus.Draft;

    private void ReplaceIngredients(IEnumerable<Ingredient> ingredients)
    {
        _ingredients.Clear();

        int position = 0;
        foreach (Ingredient ingredient in ingredients)
        {
            ingredient.SetPosition(position++);
            _ingredients.Add(ingredient);
        }
    }

    private void ReplaceInstructions(IEnumerable<InstructionStep> instructions)
    {
        _instructions.Clear();

        int position = 0;
        foreach (InstructionStep step in instructions)
        {
            step.SetPosition(position++);
            _instructions.Add(step);
        }
    }

    // Spec 014: trim, collapse internal whitespace, lowercase, drop repeats. Split(null) splits on every
    // whitespace character and RemoveEmptyEntries drops the runs, which trims and collapses in one step.
    // HashSet.Add is false for a repeat, so the first occurrence wins and the author's order survives —
    // Enumerable.Distinct does not document an order. A blank tag becomes "" and is reported by
    // ValidateProperties rather than dropped: silently discarding input is a bug, not normalisation.
    private static List<string> NormaliseTags(IEnumerable<string>? tags)
    {
        var seen = new HashSet<string>();
        return (tags ?? [])
            .Select(tag => string.Join(' ', (tag ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant())
            .Where(seen.Add)
            .ToList();
    }

    // Two tiers (spec 013): a value that is present must always be valid; completeness is demanded only of a
    // published recipe. The checks keep their original order, so the primary error ResultExtensions picks for
    // an existing payload does not change. Note the lifted operators: null < 0 and null == 0 are both false.
    private static Result ValidateProperties(RecipeStatus status, string title, string? description,
        int? preparationTime, int? cookingTime, int? servings, IReadOnlyCollection<Ingredient>? ingredients,
        IReadOnlyCollection<InstructionStep>? instructions, IReadOnlyCollection<string> tags)
    {
        var errors = new List<IError>();
        bool mustBeComplete = status == RecipeStatus.Published;

        if (string.IsNullOrWhiteSpace(title))
            errors.Add(RecipeErrors.TitleRequired());

        if (mustBeComplete && string.IsNullOrWhiteSpace(description))
            errors.Add(RecipeErrors.DescriptionRequired());

        if (mustBeComplete && preparationTime is null)
            errors.Add(RecipeErrors.PreparationTimeRequired());

        if (preparationTime < 0)
            errors.Add(RecipeErrors.PreparationTimeNegative());

        if (mustBeComplete && cookingTime is null)
            errors.Add(RecipeErrors.CookingTimeRequired());

        if (cookingTime < 0)
            errors.Add(RecipeErrors.CookingTimeNegative());

        if (mustBeComplete && preparationTime == 0 && cookingTime == 0)
            errors.Add(RecipeErrors.BothTimesZero());

        if (mustBeComplete && servings is null)
            errors.Add(RecipeErrors.ServingsRequired());

        if (servings < 1)
            errors.Add(RecipeErrors.ServingsOutOfRange(1));

        // A blank-named Ingredient cannot exist — Ingredient.Create rejects it — so the only ingredient
        // invariant left at the recipe level is "there is at least one", and only a published recipe needs one.
        if (mustBeComplete && (ingredients is null || ingredients.Count == 0))
            errors.Add(RecipeErrors.IngredientsRequired());

        // Likewise a blank-text step cannot exist — InstructionStep.Create rejects it — so what is left at the
        // recipe level is "there is at least one" (published only) and "every reference points inside this recipe".
        if (mustBeComplete && (instructions is null || instructions.Count == 0))
            errors.Add(RecipeErrors.InstructionsRequired());

        // Referential integrity is a domain invariant, not a foreign key (ADR-022): the uuid[] column has
        // nothing in the database to enforce it, so this check is the only guard — in a draft too.
        // Reported once per recipe, like IngredientsRequired: every copy would carry the same text and field.
        HashSet<Guid> ingredientIds = ingredients?.Select(i => i.Id).ToHashSet() ?? [];
        IEnumerable<Guid> referencedIds = instructions?.SelectMany(s => s.IngredientIds) ?? [];
        if (referencedIds.Any(id => !ingredientIds.Contains(id)))
            errors.Add(RecipeErrors.InstructionIngredientNotFound());

        // Normalisation has already run, so a blank tag arrives here as "". Reported once per recipe, like
        // IngredientsRequired, and in both tiers: a draft may have no tags, but never a blank one.
        if (tags.Any(tag => tag.Length == 0))
            errors.Add(RecipeErrors.TagRequired());

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(errors);
    }
}
