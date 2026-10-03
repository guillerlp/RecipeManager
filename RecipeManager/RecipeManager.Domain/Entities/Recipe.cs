using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

public sealed class Recipe : Entity
{
    private readonly List<Ingredient> _ingredients = [];
    private readonly List<InstructionStep> _instructions = [];

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public int? PreparationTime { get; private set; }
    public int? CookingTime { get; private set; }
    public int? Servings { get; private set; }
    public RecipeStatus Status { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients => _ingredients.OrderBy(i => i.Position).ToList().AsReadOnly();
    public IReadOnlyList<InstructionStep> Instructions =>
        _instructions.OrderBy(s => s.Position).ToList().AsReadOnly();

#pragma warning disable CS8618
    private Recipe()
    {
        //Constructor needed for EFCore to work properly
    }
#pragma warning restore CS8618

    private Recipe(string title, string? description, int? preparationTime, int? cookingTime, int? servings,
        List<Ingredient> ingredients, List<InstructionStep> instructions, RecipeStatus status)
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
    }

    public static Result<Recipe> Create(string title, string? description, int? preparationTime, int? cookingTime,
        int? servings, IEnumerable<Ingredient> ingredients, IEnumerable<InstructionStep> instructions,
        RecipeStatus status = RecipeStatus.Published)
    {
        // Materialise once: each list is read by ValidateProperties and again by the constructor, and an
        // IEnumerable is not guaranteed to survive being walked twice.
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];
        List<InstructionStep> instructionList = instructions?.ToList() ?? [];

        Result validate = ValidateProperties(status, title, description, preparationTime, cookingTime, servings,
            ingredientList, instructionList);

        if (validate.IsFailed)
            return Result.Fail<Recipe>(validate.Errors);

        return Result.Ok(new Recipe(title, description, preparationTime, cookingTime, servings, ingredientList,
            instructionList, status));
    }

    // Validates against the current status: a published recipe keeps the full rules on every edit.
    public Result Update(string title, string? description, int? preparationTime, int? cookingTime,
        int? servings, IEnumerable<Ingredient> ingredients, IEnumerable<InstructionStep> instructions)
    {
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];
        List<InstructionStep> instructionList = instructions?.ToList() ?? [];

        Result validate = ValidateProperties(Status, title, description, preparationTime, cookingTime, servings,
            ingredientList, instructionList);

        if (validate.IsFailed)
            return validate;

        Title = title;
        Description = description;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
        Servings = servings;
        ReplaceIngredients(ingredientList);
        ReplaceInstructions(instructionList);

        return Result.Ok();
    }

    // Idempotent: an already-published recipe met these rules on every write that got it here.
    public Result Publish()
    {
        if (Status == RecipeStatus.Published)
            return Result.Ok();

        Result validate = ValidateProperties(RecipeStatus.Published, Title, Description, PreparationTime,
            CookingTime, Servings, _ingredients, _instructions);

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

    // Two tiers (spec 013): a value that is present must always be valid; completeness is demanded only of a
    // published recipe. The checks keep their original order, so the primary error ResultExtensions picks for
    // an existing payload does not change. Note the lifted operators: null < 0 and null == 0 are both false.
    private static Result ValidateProperties(RecipeStatus status, string title, string? description,
        int? preparationTime, int? cookingTime, int? servings, IReadOnlyCollection<Ingredient>? ingredients,
        IReadOnlyCollection<InstructionStep>? instructions)
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

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(errors);
    }
}
