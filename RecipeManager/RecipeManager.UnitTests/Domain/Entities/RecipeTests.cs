using FluentAssertions;
using FluentResults;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Domain.Entities;

public class RecipeTests
{
    #region Create Method Tests - Success Scenarios

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccessResult()
    {
        // Arrange 
        var title = "Chocolate Cake";
        var description = "Delicious chocolate cake recipe";
        var preparationTime = 20;
        var cookingTime = 30;
        var servings = 8;
        var ingredients = new List<string> { "Flour", "Sugar", "Cocoa" };
        var instructions = new List<string> { "Mix ingredients", "Bake for 30 minutes" };

        // Act 
        Result<Recipe> result = Recipe.Create(title, description, preparationTime,
            cookingTime, servings, ingredients, instructions);

        // Assert 
        result.IsSuccess.Should().BeTrue(); 
        result.Value.Should().NotBeNull(); 
        result.Value.Title.Should().Be(title);
        result.Value.Description.Should().Be(description);
        result.Value.PreparationTime.Should().Be(preparationTime);
        result.Value.CookingTime.Should().Be(cookingTime);
        result.Value.Servings.Should().Be(servings);
        result.Value.Ingredients.Should().BeEquivalentTo(ingredients);
        result.Value.Instructions.Should().BeEquivalentTo(instructions);
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithOnlyPreparationTime_ShouldReturnSuccess()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            preparationTime: 10, cookingTime: 0, servings: 1, ingredients, instructions);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithOnlyCookingTime_ShouldReturnSuccess()
    {
        // Arrange 
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Bake" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            preparationTime: 0, cookingTime: 30, servings: 1, ingredients, instructions);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Create Method Tests - Validation Failures

    [Theory] 
    [InlineData(null)]
    [InlineData("")] 
    [InlineData("   ")] 
    public void Create_WithInvalidTitle_ShouldReturnFailureResult(string invalidTitle)
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create(invalidTitle, "Description",
            10, 20, 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(); 
        result.Errors.First().Message.Should().Contain("Title is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDescription_ShouldReturnFailureResult(string invalidDescription)
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", invalidDescription,
            10, 20, 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Message.Should().Contain("Description is required");
    }

    [Fact]
    public void Create_WithNegativePreparationTime_ShouldReturnFailureResult()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            preparationTime: -5, cookingTime: 20, servings: 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Preparation time cannot be negative"));
    }

    [Fact]
    public void Create_WithNegativeCookingTime_ShouldReturnFailureResult()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Bake" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            preparationTime: 10, cookingTime: -10, servings: 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Cooking time cannot be negative"));
    }

    [Fact]
    public void Create_WithBothTimesZero_ShouldReturnFailureResult()
    {
        // Arrange 
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            preparationTime: 0, cookingTime: 0, servings: 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Message.Contains("At least one of preparation or cooking time must be greater than 0"));
    }

    [Theory]
    [InlineData(0)]  
    [InlineData(-1)] 
    public void Create_WithInvalidServings_ShouldReturnFailureResult(int invalidServings)
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, invalidServings, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Servings must be a least 1"));
    }

    [Fact]
    public void Create_WithEmptyIngredientsList_ShouldReturnFailureResult()
    {
        // Arrange
        var emptyIngredients = new List<string>(); 
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, 2, emptyIngredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("At least one ingredient is required"));
    }

    [Fact]
    public void Create_WithEmptyIngredientString_ShouldReturnFailureResult()
    {
        // Arrange
        var ingredientsWithEmpty = new List<string> { "Flour", "  ", "Sugar" }; 
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, 2, ingredientsWithEmpty, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Ingredients cannot be empty"));
    }

    [Fact]
    public void Create_WithEmptyInstructionsList_ShouldReturnFailureResult()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var emptyInstructions = new List<string>();

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, 2, ingredients, emptyInstructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("At least one instruction step is required"));
    }

    [Fact]
    public void Create_WithEmptyInstructionString_ShouldReturnFailureResult()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructionsWithEmpty = new List<string> { "Mix", "", "Bake" };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, 2, ingredients, instructionsWithEmpty);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Instruction steps cannot be empty"));
    }

    [Fact]
    public void Create_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange 
        var emptyIngredients = new List<string>();
        var emptyInstructions = new List<string>();

        // Act
        Result<Recipe> result = Recipe.Create("", "", // Invalid title and description
            -5, -10, // Invalid times
            0, // Invalid servings
            emptyIngredients, emptyInstructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1); 
        result.Errors.Should().Contain(e => e.Message.Contains("Title"));
        result.Errors.Should().Contain(e => e.Message.Contains("Description"));
        result.Errors.Should().Contain(e => e.Message.Contains("Preparation time"));
        result.Errors.Should().Contain(e => e.Message.Contains("Cooking time"));
        result.Errors.Should().Contain(e => e.Message.Contains("Servings"));
        result.Errors.Should().Contain(e => e.Message.Contains("ingredient"));
        result.Errors.Should().Contain(e => e.Message.Contains("instruction"));
    }

    #endregion

    #region Update Method Tests

    [Fact]
    public void Update_WithValidData_ShouldUpdatePropertiesAndReturnSuccess()
    {
        // Arrange 
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };
        Result<Recipe> createResult = Recipe.Create("Original Title", "Original Description",
            10, 20, 2, ingredients, instructions);
        var recipe = createResult.Value;

        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newIngredients = new List<string> { "Sugar", "Butter" };
        var newInstructions = new List<string> { "Cream butter", "Add sugar" };

        // Act 
        Result updateResult = recipe.Update(newTitle, newDescription,
            15, 25, 4, newIngredients, newInstructions);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        recipe.Title.Should().Be(newTitle);
        recipe.Description.Should().Be(newDescription);
        recipe.PreparationTime.Should().Be(15);
        recipe.CookingTime.Should().Be(25);
        recipe.Servings.Should().Be(4);
        recipe.Ingredients.Should().BeEquivalentTo(newIngredients);
        recipe.Instructions.Should().BeEquivalentTo(newInstructions);
    }

    [Fact]
    public void Update_WithInvalidData_ShouldNotUpdatePropertiesAndReturnFailure()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };
        Result<Recipe> createResult = Recipe.Create("Original Title", "Original Description",
            10, 20, 2, ingredients, instructions);
        var recipe = createResult.Value;

        var originalTitle = recipe.Title;
        var originalDescription = recipe.Description;

        // Act
        Result updateResult = recipe.Update("", "", // Invalid title and description
            -5, 20, 2, ingredients, instructions);

        // Assert
        updateResult.IsFailed.Should().BeTrue();
        recipe.Title.Should().Be(originalTitle);
        recipe.Description.Should().Be(originalDescription);
    }

    #endregion

    #region Error Metadata Tests

    [Fact]
    public void Create_WithInvalidTitle_ShouldIncludeErrorCodeInMetadata()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("", "Description",
            10, 20, 2, ingredients, instructions);

        // Assert 
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.First();
        error.Metadata.Should().ContainKey("ErrorCode");
        error.Metadata["ErrorCode"].Should().Be(422); 
    }

    [Fact]
    public void Create_WithInvalidData_ShouldIncludeFieldNameInMetadata()
    {
        // Arrange
        var ingredients = new List<string> { "Flour" };
        var instructions = new List<string> { "Mix" };

        // Act
        Result<Recipe> result = Recipe.Create("", "Description",
            10, 20, 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.First();
        error.Metadata.Should().ContainKey("field");
        error.Metadata["field"].Should().Be("title");
    }

    #endregion
}