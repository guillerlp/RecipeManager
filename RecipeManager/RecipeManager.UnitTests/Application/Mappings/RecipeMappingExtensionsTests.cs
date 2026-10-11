using FluentAssertions;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Mappings;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Interfaces.Repositories;

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

    [Fact]
    public void MapToRecipeDto_WithoutStats_ShouldSayNeverCooked()
    {
        RecipeDto dto = Recipe.Create("Title", null, null, null, null, [], [], RecipeStatus.Draft).Value
            .MapToRecipeDto();

        dto.CookCount.Should().Be(0);
        dto.LastCookedAt.Should().BeNull();
    }

    [Fact]
    public void MapToRecipeDto_WithStats_ShouldCarryThem()
    {
        var last = new DateTimeOffset(2026, 10, 8, 19, 0, 0, TimeSpan.Zero);

        RecipeDto dto = Recipe.Create("Title", null, null, null, null, [], [], RecipeStatus.Draft).Value
            .MapToRecipeDto(new CookStats(11, last));

        dto.CookCount.Should().Be(11);
        dto.LastCookedAt.Should().Be(last);
    }
}
