using Ardalis.GuardClauses;
using RecipeManager.Domain.Shared;

namespace RecipeManager.Domain.Entities
{
    public sealed class Recipe : Entity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }
        public List<string> Ingredients { get; set; }
        public List<string> Instructions { get; set; }

#pragma warning disable CS8618
        private Recipe()
        {
            //Constructor needed for EFCore to work properly
        }
#pragma warning restore CS8618 

        private Recipe(string title, string description, int preparationTime, int cookingTime, int servings, List<string> ingredients, List<string> instructions)
        {
            Id = Guid.NewGuid();
            Title = title;
            Description = description;
            PreparationTime = preparationTime;
            CookingTime = cookingTime;
            Servings = servings;
            Ingredients = ingredients;
            Instructions = instructions;
        }
        public static Recipe Create(string title, string description, int preparationTime, int cookingTime, int servings, List<string> ingredients, List<string> instructions)
        {
            Guard.Against.NullOrWhiteSpace(title, nameof(title), "Title is required.");
            Guard.Against.NullOrWhiteSpace(description, nameof(description), "Description is required.");
            Guard.Against.Negative(preparationTime, nameof(preparationTime), "Preparation time must be at least 0");
            Guard.Against.Negative(cookingTime, nameof(cookingTime), "Cooking time must be at least 0");
            Guard.Against.NegativeOrZero(servings, nameof(servings), "Servings must be superior to 0");
            Guard.Against.NullOrEmpty(ingredients, nameof(ingredients), "At least one ingredient is necessary.");
            Guard.Against.NullOrEmpty(instructions, nameof(instructions), "At least one instruction is necessary.");

            return new Recipe(title, description, preparationTime, cookingTime, servings, ingredients, instructions);
        }
    }
}
