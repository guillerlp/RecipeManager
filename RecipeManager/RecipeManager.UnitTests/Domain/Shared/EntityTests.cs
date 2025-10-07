using FluentAssertions;
using RecipeManager.Domain.Entities;

namespace RecipeManager.UnitTests.Domain.Shared;

/// <summary>
/// Unit tests for Entity base class
/// Tests the equality comparison and GetHashCode implementation
/// </summary>
public class EntityTests
{
    #region Equals Tests

    [Fact]
    public void Equals_WithSameIdAndType_ShouldReturnTrue()
    {
        // Arrange - Create two Recipe entities with the same ID
        var sharedId = Guid.NewGuid();

        var recipe1Result = Recipe.Create(
            "Recipe 1", "Description 1", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );
        var recipe1 = recipe1Result.Value;

        var recipe2Result = Recipe.Create(
            "Recipe 2", "Description 2", 15, 25, 4,
            new List<string> { "Sugar" },
            new List<string> { "Bake" }
        );
        var recipe2 = recipe2Result.Value;

        // Manually set the same ID using reflection (since Id is protected init)
        var idProperty = typeof(Recipe).BaseType!.GetProperty("Id")!;
        idProperty.SetValue(recipe1, sharedId);
        idProperty.SetValue(recipe2, sharedId);

        // Act
        bool areEqual = recipe1.Equals(recipe2);

        // Assert
        areEqual.Should().BeTrue("entities with the same ID should be equal");
    }

    [Fact]
    public void Equals_WithDifferentIds_ShouldReturnFalse()
    {
        // Arrange 
        var recipe1Result = Recipe.Create(
            "Recipe 1", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        var recipe2Result = Recipe.Create(
            "Recipe 2", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        // Act
        bool areEqual = recipe1Result.Value.Equals(recipe2Result.Value);

        // Assert
        areEqual.Should().BeFalse("entities with different IDs should not be equal");
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var recipeResult = Recipe.Create(
            "Recipe", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        // Act
        bool areEqual = recipeResult.Value.Equals(null);

        // Assert
        areEqual.Should().BeFalse("entity should not equal null");
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var recipeResult = Recipe.Create(
            "Recipe", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        var someObject = new object();

        // Act
        bool areEqual = recipeResult.Value.Equals(someObject);

        // Assert
        areEqual.Should().BeFalse("entity should not equal object of different type");
    }

    [Fact]
    public void Equals_WithSameInstance_ShouldReturnTrue()
    {
        // Arrange
        var recipeResult = Recipe.Create(
            "Recipe", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );
        var recipe = recipeResult.Value;

        // Act
        bool areEqual = recipe.Equals(recipe);

        // Assert
        areEqual.Should().BeTrue("entity should equal itself");
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_ForSameEntity_ShouldReturnSameHashCode()
    {
        // Arrange
        var recipeResult = Recipe.Create(
            "Recipe", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );
        var recipe = recipeResult.Value;

        // Act
        int hashCode1 = recipe.GetHashCode();
        int hashCode2 = recipe.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2, "same entity should return same hash code");
    }

    [Fact]
    public void GetHashCode_ForEntitiesWithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var sharedId = Guid.NewGuid();

        var recipe1Result = Recipe.Create(
            "Recipe 1", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        var recipe2Result = Recipe.Create(
            "Recipe 2", "Different Description", 15, 25, 4,
            new List<string> { "Sugar" },
            new List<string> { "Bake" }
        );

        // Set same ID
        var idProperty = typeof(Recipe).BaseType!.GetProperty("Id")!;
        idProperty.SetValue(recipe1Result.Value, sharedId);
        idProperty.SetValue(recipe2Result.Value, sharedId);

        // Act
        int hashCode1 = recipe1Result.Value.GetHashCode();
        int hashCode2 = recipe2Result.Value.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2, "entities with same ID should have same hash code");
    }

    [Fact]
    public void GetHashCode_ForEntitiesWithDifferentIds_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var recipe1Result = Recipe.Create(
            "Recipe 1", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        var recipe2Result = Recipe.Create(
            "Recipe 2", "Description", 10, 20, 2,
            new List<string> { "Flour" },
            new List<string> { "Mix" }
        );

        // Act
        int hashCode1 = recipe1Result.Value.GetHashCode();
        int hashCode2 = recipe2Result.Value.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2, "entities with different IDs should likely have different hash codes");
    }

    #endregion
}
