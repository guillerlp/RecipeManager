namespace RecipeManager.Api.Startup.CustomObjects
{
    public record DatabaseConnectionConfiguration
    {
        public string DefaultConnection { get; init; } = string.Empty;
    }
}
