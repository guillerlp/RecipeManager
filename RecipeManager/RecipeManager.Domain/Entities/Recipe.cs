using Ardalis.GuardClauses;
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

        private Recipe(string title, string description, int preparationTime, int cookingTime, int servings, IEnumerable<string> ingredients, IEnumerable<string> instructions)
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

        public static Recipe Create(string title, string description, int preparationTime, int cookingTime, int servings, IEnumerable<string> ingredients, IEnumerable<string> instructions)
        {
            ValidateProperties(title, description, preparationTime, cookingTime, servings, ingredients, instructions);

            return new Recipe(title, description, preparationTime, cookingTime, servings, ingredients, instructions);
        }

        public void Update(string title, string description, int preparationTime, int cookingTime, int servings, IEnumerable<string> ingredients, IEnumerable<string> instructions)
        {
            ValidateProperties(title, description, preparationTime, cookingTime, servings, ingredients, instructions);

            Title = title;
            Description = description;
            PreparationTime = preparationTime;
            CookingTime = cookingTime;
            Servings = servings;
            Ingredients = ingredients.ToList().AsReadOnly();
            Instructions = instructions.ToList().AsReadOnly();
        }

        private static void ValidateProperties(string title, string description, int preparationTime, int cookingTime, int servings, IEnumerable<string> ingredients, IEnumerable<string> instructions)
        {
            Guard.Against.NullOrWhiteSpace(title, nameof(title), "Title is required");
            Guard.Against.NullOrWhiteSpace(description, nameof(description), "Description is required");
            Guard.Against.Negative(preparationTime, nameof(preparationTime), "Preparation time must be at least 0");
            Guard.Against.Negative(cookingTime, nameof(cookingTime), "Cooking time must be at least 0");
            Guard.Against.NegativeOrZero(servings, nameof(servings), "Servings must be superior to 0");
            Guard.Against.NullOrEmpty(ingredients, nameof(ingredients), "At least one ingredient is required");
            Guard.Against.NullOrEmpty(instructions, nameof(instructions), "At least one instruction is required");
        }
    }
}
