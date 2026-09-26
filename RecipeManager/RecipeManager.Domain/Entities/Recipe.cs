using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

public sealed class Recipe : Entity
{
    private readonly List<Ingredient> _ingredients = [];
    private readonly List<InstructionStep> _instructions = [];

    public string Title { get; private set; }
    public string Description { get; private set; }
    public int PreparationTime { get; private set; }
    public int CookingTime { get; private set; }
    public int Servings { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients => _ingredients.OrderBy(i => i.Position).ToList().AsReadOnly();
    public IReadOnlyList<InstructionStep> Instructions =>
        _instructions.OrderBy(s => s.Position).ToList().AsReadOnly();

#pragma warning disable CS8618
    private Recipe()
    {
        //Constructor needed for EFCore to work properly
    }
#pragma warning restore CS8618

    private Recipe(string title, string description, int preparationTime, int cookingTime, int servings,
        List<Ingredient> ingredients, List<InstructionStep> instructions)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
        Servings = servings;
        ReplaceIngredients(ingredients);
        ReplaceInstructions(instructions);
    }

    public static Result<Recipe> Create(string title, string description, int preparationTime, int cookingTime,
        int servings, IEnumerable<Ingredient> ingredients, IEnumerable<InstructionStep> instructions)
    {
        // Materialise once: each list is read by ValidateProperties and again by the constructor, and an
        // IEnumerable is not guaranteed to survive being walked twice.
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];
        List<InstructionStep> instructionList = instructions?.ToList() ?? [];

        Result validate = ValidateProperties(title, description, preparationTime, cookingTime, servings,
            ingredientList, instructionList);

        if (validate.IsFailed)
            return Result.Fail<Recipe>(validate.Errors);

        return Result.Ok(new Recipe(title, description, preparationTime, cookingTime, servings, ingredientList,
            instructionList));
    }

    public Result Update(string title, string description, int preparationTime, int cookingTime, int servings,
        IEnumerable<Ingredient> ingredients, IEnumerable<InstructionStep> instructions)
    {
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];
        List<InstructionStep> instructionList = instructions?.ToList() ?? [];

        Result validate = ValidateProperties(title, description, preparationTime, cookingTime, servings,
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

    private static Result ValidateProperties(string title, string description, int preparationTime,
        int cookingTime, int servings, IReadOnlyCollection<Ingredient>? ingredients,
        IReadOnlyCollection<InstructionStep>? instructions)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(title))
            errors.Add(RecipeErrors.TitleRequired());

        if (string.IsNullOrWhiteSpace(description))
            errors.Add(RecipeErrors.DescriptionRequired());

        if (preparationTime < 0)
            errors.Add(RecipeErrors.PreparationTimeNegative());

        if (cookingTime < 0)
            errors.Add(RecipeErrors.CookingTimeNegative());

        if (preparationTime == 0 && cookingTime == 0)
            errors.Add(RecipeErrors.BothTimesZero());

        if (servings < 1)
            errors.Add(RecipeErrors.ServingsOutOfRange(1));

        // A blank-named Ingredient cannot exist — Ingredient.Create rejects it — so the only ingredient
        // invariant left at the recipe level is "there is at least one".
        if (ingredients is null || ingredients.Count == 0)
            errors.Add(RecipeErrors.IngredientsRequired());

        // Likewise a blank-text step cannot exist — InstructionStep.Create rejects it — so what is left at the
        // recipe level is "there is at least one" and "every reference points inside this recipe".
        if (instructions is null || instructions.Count == 0)
            errors.Add(RecipeErrors.InstructionsRequired());

        // Referential integrity is a domain invariant, not a foreign key (ADR-022): the uuid[] column has
        // nothing in the database to enforce it, so this check is the only guard.
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
