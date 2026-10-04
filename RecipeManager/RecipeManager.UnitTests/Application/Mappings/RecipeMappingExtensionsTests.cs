using FluentAssertions;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Domain.Entities;

namespace RecipeManager.UnitTests.Application.Mappings;

public class RecipeMappingExtensionsTests
{
    [Fact]
    public void MapToRecipeDto_ShouldCarryTheTagsInOrder()
    {
        Recipe recipe = Recipe.Create("Title", null, null, null, null, [], [], RecipeStatus.Draft,
            ["roast", "feeds a table"]).Value;

        RecipeDto dto = recipe.MapToRecipeDto();

        dto.Tags.Should().Equal("roast", "feeds a table");
    }
}
