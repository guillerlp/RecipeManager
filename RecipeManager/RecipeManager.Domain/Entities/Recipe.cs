namespace RecipeManager.Domain.Entities
{
    public class Recipe
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }
        public List<string> Ingredients { get; set; }
        public List<string> Instructions { get; set; }

        public Recipe(string title, string description, int preparationTime, int cookingTime, int servings, List<string> ingredients, List<string> instructions)
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

#pragma warning disable CS8618 
        public Recipe()
        {
            //Constructor needed for EFCore to work properly
        }
#pragma warning restore CS8618 
    }
}
