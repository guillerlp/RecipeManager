using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities;

public sealed class Recipe : Entity
{
    private readonly List<Ingredient> _ingredients = [];

    public string Title { get; private set; }
    public string Description { get; private set; }
    public int PreparationTime { get; private set; }
    public int CookingTime { get; private set; }
    public int Servings { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients => _ingredients.OrderBy(i => i.Position).ToList().AsReadOnly();
    public IReadOnlyList<string> Instructions { get; private set; }

#pragma warning disable CS8618
    private Recipe()
    {
        //Constructor needed for EFCore to work properly
    }
#pragma warning restore CS8618

    private Recipe(string title, string description, int preparationTime, int cookingTime, int servings,
        List<Ingredient> ingredients, IEnumerable<string> instructions)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
        Servings = servings;
        ReplaceIngredients(ingredients);
        Instructions = instructions.ToList().AsReadOnly();
    }

    public static Result<Recipe> Create(string title, string description, int preparationTime, int cookingTime,
        int servings, IEnumerable<Ingredient> ingredients, IEnumerable<string> instructions)
    {
        // Materialise once: the list is read by ValidateProperties and again by the constructor, and an
        // IEnumerable is not guaranteed to survive being walked twice.
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];

        Result validate = ValidateProperties(title, description, preparationTime, cookingTime, servings,
            ingredientList, instructions);

        if (validate.IsFailed)
            return Result.Fail<Recipe>(validate.Errors);

        return Result.Ok(new Recipe(title, description, preparationTime, cookingTime, servings, ingredientList,
            instructions));
    }

    public Result Update(string title, string description, int preparationTime, int cookingTime, int servings,
        IEnumerable<Ingredient> ingredients, IEnumerable<string> instructions)
    {
        List<Ingredient> ingredientList = ingredients?.ToList() ?? [];

        Result validate = ValidateProperties(title, description, preparationTime, cookingTime, servings,
            ingredientList, instructions);

        if (validate.IsFailed)
            return validate;

        Title = title;
        Description = description;
        PreparationTime = preparationTime;
        CookingTime = cookingTime;
        Servings = servings;
        ReplaceIngredients(ingredientList);
        Instructions = instructions.ToList().AsReadOnly();

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

    private static Result ValidateProperties(string title, string description, int preparationTime,
        int cookingTime, int servings, IReadOnlyCollection<Ingredient>? ingredients,
        IEnumerable<string>? instructions)
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

        var steps = instructions?.ToList() ?? new List<string>();
        if (steps.Count == 0)
        {
            errors.Add(RecipeErrors.InstructionsRequired());
        }
        else
        {
            if (steps.Any(s => string.IsNullOrWhiteSpace(s)))
                errors.Add(RecipeErrors.InstructionEmpty());
        }

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(errors);
    }
}
