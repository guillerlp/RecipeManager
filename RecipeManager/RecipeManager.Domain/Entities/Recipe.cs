using FluentResults;
using RecipeManager.Domain.Errors;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities
{
    public sealed class Recipe : Entity
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public int PreparationTime { get; private set; }
        public int CookingTime { get; private set; }
        public int Servings { get; private set; }
        public IReadOnlyList<string> Ingredients { get; private set; }
        public IReadOnlyList<string> Instructions { get; private set; }

#pragma warning disable CS8618
        private Recipe()
        {
            //Constructor needed for EFCore to work properly
        }
#pragma warning restore CS8618

        private Recipe(string title, string description, int preparationTime, int cookingTime, int servings,
            IEnumerable<string> ingredients, IEnumerable<string> instructions)
        {
            Id = Guid.NewGuid();
            Title = title;
            Description = description;
            PreparationTime = preparationTime;
            CookingTime = cookingTime;
            Servings = servings;
            Ingredients = ingredients.ToList().AsReadOnly();
            Instructions = instructions.ToList().AsReadOnly();
        }

        public static Result<Recipe> Create(string title, string description, int preparationTime, int cookingTime,
            int servings, IEnumerable<string> ingredients, IEnumerable<string> instructions)
        {
            Result validate = ValidateProperties(title, description, preparationTime, cookingTime, servings,
                ingredients, instructions);

            if (validate.IsFailed)
                return Result.Fail<Recipe>(validate.Errors);

            return Result.Ok(new Recipe(title, description, preparationTime, cookingTime, servings, ingredients,
                instructions));
        }

        public Result Update(string title, string description, int preparationTime, int cookingTime, int servings,
            IEnumerable<string> ingredients, IEnumerable<string> instructions)
        {
            Result validate = ValidateProperties(title, description, preparationTime, cookingTime, servings,
                ingredients, instructions);

            if (validate.IsFailed)
                return validate;

            Title = title;
            Description = description;
            PreparationTime = preparationTime;
            CookingTime = cookingTime;
            Servings = servings;
            Ingredients = ingredients.ToList().AsReadOnly();
            Instructions = instructions.ToList().AsReadOnly();

            return Result.Ok();
        }

        private static Result ValidateProperties(string title, string description, int preparationTime,
            int cookingTime, int servings, IEnumerable<string>? ingredients, IEnumerable<string>? instructions)
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

            var ingList = ingredients?.ToList() ?? new List<string>();
            if (ingList.Count == 0)
            {
                errors.Add(RecipeErrors.IngredientsRequired());
            }
            else
            {
                if (ingList.Any(s => string.IsNullOrWhiteSpace(s)))
                    errors.Add(RecipeErrors.IngredientEmpty());
            }

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
}