using FluentAssertions;
using FluentResults;
using RecipeManager.Domain.Entities;
using RecipeManager.Domain.Errors;

namespace RecipeManager.UnitTests.Domain.Entities;

public class RecipeTests
{
    private static Ingredient Ing(string name, decimal? quantity = null, Unit? unit = null) =>
        Ingredient.Create(null, quantity, unit, name, null).Value;

    private static InstructionStep Step(string text) => InstructionStep.Create(text, null, []).Value;

    private static InstructionStep StepUsing(string text, params Ingredient[] ingredients) =>
        InstructionStep.Create(text, null, ingredients.Select(i => i.Id)).Value;

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
        var ingredients = new List<Ingredient> { Ing("Flour"), Ing("Sugar"), Ing("Cocoa") };
        var instructions = new List<InstructionStep> { Step("Mix ingredients"), Step("Bake for 30 minutes") };

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
        result.Value.Ingredients.Select(i => i.Name).Should().Equal("Flour", "Sugar", "Cocoa");
        result.Value.Instructions.Select(s => s.Text).Should().Equal("Mix ingredients", "Bake for 30 minutes");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithOnlyPreparationTime_ShouldReturnSuccess()
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

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
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Bake") };

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
    public void Create_WithInvalidTitle_ShouldReturnFailureResult(string? invalidTitle)
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

        // Act
        Result<Recipe> result = Recipe.Create(invalidTitle!, "Description",
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
    public void Create_WithInvalidDescription_ShouldReturnFailureResult(string? invalidDescription)
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

        // Act
        Result<Recipe> result = Recipe.Create("Title", invalidDescription!,
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
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

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
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Bake") };

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
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

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
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

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
        var emptyIngredients = new List<Ingredient>();
        var instructions = new List<InstructionStep> { Step("Mix") };

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, 2, emptyIngredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("At least one ingredient is required"));
    }

    [Fact]
    public void Create_WithEmptyInstructionsList_ShouldReturnFailureResult()
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var emptyInstructions = new List<InstructionStep>();

        // Act
        Result<Recipe> result = Recipe.Create("Title", "Description",
            10, 20, 2, ingredients, emptyInstructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("At least one instruction step is required"));
    }

    [Fact]
    public void Create_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var emptyIngredients = new List<Ingredient>();
        var emptyInstructions = new List<InstructionStep>();

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
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };
        Result<Recipe> createResult = Recipe.Create("Original Title", "Original Description",
            10, 20, 2, ingredients, instructions);
        var recipe = createResult.Value;

        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newIngredients = new List<Ingredient> { Ing("Sugar"), Ing("Butter") };
        var newInstructions = new List<InstructionStep> { Step("Cream butter"), Step("Add sugar") };

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
        recipe.Ingredients.Select(i => i.Name).Should().Equal("Sugar", "Butter");
        recipe.Instructions.Select(s => s.Text).Should().Equal("Cream butter", "Add sugar");
    }

    [Fact]
    public void Update_WithInvalidData_ShouldNotUpdatePropertiesAndReturnFailure()
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };
        Result<Recipe> createResult = Recipe.Create("Original Title", "Original Description",
            10, 20, 2, ingredients, instructions);
        var recipe = createResult.Value;

        var originalTitle = recipe.Title;
        var originalDescription = recipe.Description;

        // Act
        Result updateResult = recipe.Update("", "", // Invalid title and description
            -5, 20, 2, ingredients, [Step("Replaced")]);

        // Assert
        updateResult.IsFailed.Should().BeTrue();
        recipe.Title.Should().Be(originalTitle);
        recipe.Description.Should().Be(originalDescription);
        recipe.Instructions.Select(s => s.Text).Should().Equal("Mix");
    }

    #endregion

    #region Error Metadata Tests

    [Fact]
    public void Create_WithInvalidTitle_ShouldReturnValidationKindWithoutHttpCode()
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

        // Act
        Result<Recipe> result = Recipe.Create("", "Description",
            10, 20, 2, ingredients, instructions);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.First();
        error.Should().BeOfType<DomainError>()
            .Which.Kind.Should().Be(ErrorKind.Validation);
        error.Metadata.Should().NotContainKey("ErrorCode");
    }

    [Fact]
    public void Create_WithInvalidData_ShouldIncludeFieldNameInMetadata()
    {
        // Arrange
        var ingredients = new List<Ingredient> { Ing("Flour") };
        var instructions = new List<InstructionStep> { Step("Mix") };

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

    #region Structured Ingredients

    [Fact]
    public void Create_ShouldAssignPositionsFromListOrder()
    {
        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1,
            [Ing("Flour"), Ing("Sugar"), Ing("Cocoa")], [Step("Mix")]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Ingredients.Select(i => i.Position).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Ingredients_ShouldBeReturnedInPositionOrder()
    {
        // Should().Equal is order-SENSITIVE. BeEquivalentTo is not, and would pass on a shuffled list —
        // that is exactly the hole TEST-03 records for instructions.
        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1,
            [Ing("Flour"), Ing("Sugar"), Ing("Cocoa")], [Step("Mix")]);

        result.Value.Ingredients.Select(i => i.Name).Should().Equal("Flour", "Sugar", "Cocoa");
    }

    [Fact]
    public void Update_ShouldReassignPositionsFromTheNewOrder()
    {
        Recipe recipe = Recipe.Create("Title", "Description", 10, 0, 1,
            [Ing("Flour"), Ing("Sugar")], [Step("Mix")]).Value;

        Result result = recipe.Update("Title", "Description", 10, 0, 1,
            [Ing("Sugar"), Ing("Flour")], [Step("Mix")]);

        result.IsSuccess.Should().BeTrue();
        recipe.Ingredients.Select(i => i.Name).Should().Equal("Sugar", "Flour");
        recipe.Ingredients.Select(i => i.Position).Should().Equal(0, 1);
    }

    [Fact]
    public void Update_ShouldPreserveSuppliedIngredientIds()
    {
        // This is what keeps R-17's step references valid across an edit.
        var keptId = Guid.NewGuid();
        Recipe recipe = Recipe.Create("Title", "Description", 10, 0, 1, [Ing("Flour")], [Step("Mix")]).Value;

        recipe.Update("Title", "Description", 10, 0, 1,
            [Ingredient.Create(keptId, 1m, Unit.Cup, "Flour", null).Value], [Step("Mix")]);

        recipe.Ingredients.Should().ContainSingle().Which.Id.Should().Be(keptId);
    }

    [Fact]
    public void Create_WithNoIngredients_ShouldFailWithIngredientsRequired()
    {
        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1, [], [Step("Mix")]);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == RecipeErrors.IngredientsRequired().Message);
    }

    [Fact]
    public void Ingredients_ShouldNotBeCastableToAMutableList()
    {
        // BUG-11, ingredient half. Ingredients wraps its projection in AsReadOnly(), so callers get a
        // ReadOnlyCollection rather than a real List<Ingredient> — a bare ToList() would still be
        // castable back to List<Ingredient> and this assertion would fail.
        Recipe recipe = Recipe.Create("Title", "Description", 10, 0, 1, [Ing("Flour")], [Step("Mix")]).Value;

        recipe.Ingredients.Should().NotBeAssignableTo<List<Ingredient>>();
    }

    #endregion

    #region Structured Instructions

    [Fact]
    public void Create_ShouldAssignStepPositionsFromListOrder()
    {
        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1,
            [Ing("Flour")], [Step("Mix"), Step("Rest"), Step("Bake")]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Instructions.Select(s => s.Position).Should().Equal(0, 1, 2);
        result.Value.Instructions.Select(s => s.Text).Should().Equal("Mix", "Rest", "Bake");
    }

    [Fact]
    public void Update_ShouldReplaceStepsAndReassignPositions()
    {
        Recipe recipe = Recipe.Create("Title", "Description", 10, 0, 1,
            [Ing("Flour")], [Step("Mix"), Step("Bake")]).Value;

        Result result = recipe.Update("Title", "Description", 10, 0, 1,
            [Ing("Flour")], [Step("Bake"), Step("Mix"), Step("Serve")]);

        result.IsSuccess.Should().BeTrue();
        recipe.Instructions.Select(s => s.Text).Should().Equal("Bake", "Mix", "Serve");
        recipe.Instructions.Select(s => s.Position).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Create_WithAStepReferencingThisRecipesIngredient_ShouldSucceed()
    {
        Ingredient flour = Ing("Flour");

        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1,
            [flour], [StepUsing("Sift", flour)]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Instructions.Single().IngredientIds.Should().Equal(flour.Id);
    }

    [Fact]
    public void Create_WithAStepReferencingAnUnknownIngredient_ShouldFailWithInstructionIngredientNotFound()
    {
        Ingredient flour = Ing("Flour");
        Ingredient stranger = Ing("Not in this recipe");

        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1,
            [flour], [StepUsing("Sift", flour, stranger)]);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(RecipeErrors.InstructionIngredientNotFound().Message);
    }

    [Fact]
    public void Update_RemovingAnIngredientAStillReferences_ShouldFailAndChangeNothing()
    {
        // The invariant must be checked against the NEW ingredient list, not the stored one.
        Ingredient flour = Ing("Flour");
        Ingredient butter = Ing("Butter");
        Recipe recipe = Recipe.Create("Title", "Description", 10, 0, 1,
            [flour, butter], [StepUsing("Rub in", butter)]).Value;

        Result result = recipe.Update("Title", "Description", 10, 0, 1,
            [flour], [StepUsing("Rub in", butter)]);

        result.IsFailed.Should().BeTrue();
        recipe.Ingredients.Select(i => i.Name).Should().Equal("Flour", "Butter");
    }

    [Fact]
    public void Create_WithNoStepsAndNoIngredients_ShouldReportBothRequiredErrorsAndNoReferenceError()
    {
        Result<Recipe> result = Recipe.Create("Title", "Description", 10, 0, 1, [], []);

        result.Errors.Select(e => e.Message).Should().BeEquivalentTo(
            RecipeErrors.IngredientsRequired().Message,
            RecipeErrors.InstructionsRequired().Message);
    }

    [Fact]
    public void Instructions_ShouldNotBeCastableToAMutableList()
    {
        // BUG-11, instruction half — the in-memory side. Task 3 pins the materialised-from-PostgreSQL side.
        Recipe recipe = Recipe.Create("Title", "Description", 10, 0, 1, [Ing("Flour")], [Step("Mix")]).Value;

        recipe.Instructions.Should().NotBeAssignableTo<List<InstructionStep>>();
    }

    #endregion
}
