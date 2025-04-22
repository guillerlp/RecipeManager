namespace RecipeManager.Application.DTO.Recipes
{
    public sealed class RecipeDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }
        public List<string> Ingredients { get; set; }

        public List<string> Instructions;

        public RecipeDto(Guid id, string title, string description, int preparationTime, int cookingTime, int servings, List<string> ingredients, List<string> instructions)
        {
            Id = id;
            Title = title;
            Description = description;
            PreparationTime = preparationTime;
            CookingTime = cookingTime;
            Servings = servings;
            Ingredients = ingredients;
            Instructions = instructions;
        }
    }
}
