using FluentAssertions;
using RecipeManager.Domain.Entities;
using System.Collections.ObjectModel;
using Xunit;

namespace RecipeManager.Domain.Tests.Entities
{
    public class RecipeTests
    {
#pragma warning disable CS8625
        public static IEnumerable<object[]> InvalidListSets =>
            new List<object[]>
            {
              new object[] { null },
              new object[] { new List<string>() }
            };
#pragma warning restore CS8625

        [Fact]
        public void Create_WithValidArguments_SetsAllProperties()
        {
            var ingredientsList = new List<string> { "Eggs", "Flour" };
            var instructionsList = new List<string> { "Cut", "Bake" };

            var recipe = Recipe.Create(
                title: "Pancakes",
                description: "Easy",
                preparationTime: 10,
                cookingTime: 20,
                servings: 4,
                ingredients: ingredientsList,
                instructions: instructionsList
            );

            recipe.Id.Should().NotBeEmpty();
            recipe.Title.Should().Be("Pancakes");
            recipe.Description.Should().Be("Easy");
            recipe.PreparationTime.Should().Be(10);
            recipe.CookingTime.Should().Be(20);
            recipe.Servings.Should().Be(4);
            recipe.Ingredients.Should().Equal(ingredientsList);
            recipe.Ingredients.Should().BeOfType<ReadOnlyCollection<string>>();
            recipe.Instructions.Should().Equal(instructionsList);
        }

        [Fact]
        public void Create_IsDefensive_Copy_OnLists()
        {
            var ingredientsList = new List<string> { "Eggs", "Flour" };
            var instructionsList = new List<string> { "Cut", "Bake" };

            var recipe = Recipe.Create(
                title: "Pancakes",
                description: "Easy",
                preparationTime: 10,
                cookingTime: 20,
                servings: 4,
                ingredients: ingredientsList,
                instructions: instructionsList
            );

            ingredientsList.Add("Milk");
            instructionsList.Add("Mix");

            recipe.Ingredients.Should().NotContain("Milk");
            recipe.Instructions.Should().NotContain("Mix");
        }

#pragma warning disable xUnit1012 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
#pragma warning restore xUnit1012 
        public void Create_InvalidTitle_Throws_ArgumentException(string bad)
        {
            var action = () => { Recipe.Create(bad, "D", 0, 0, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Title is required*");
        }

#pragma warning disable xUnit1012
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
#pragma warning restore xUnit1012 
        public void Create_InvalidDescription_Throws_ArgumentException(string bad)
        {
            var action = () => { Recipe.Create("T", bad, 0, 0, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Description is required*");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Create_InvalidPreparationTime_Throws_ArgumentException(int bad)
        {
            var action = () => { Recipe.Create("T", "D", bad, 0, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Preparation time must be at least 0*");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Create_InvalidCookingTime_Throws_ArgumentException(int bad)
        {
            var action = () => { Recipe.Create("T", "D", 0, bad, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Cooking time must be at least 0*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Create_InvalidServings_Throws_ArgumentException(int bad)
        {
            var action = () => { Recipe.Create("T", "D", 0, 0, bad, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Servings must be superior to 0*");
        }

        [Theory]
        [MemberData(nameof(InvalidListSets))]
        public void Create_InvalidIngredients_Throws_ArgumentException(IEnumerable<string> bad)
        {
            var action = () => { Recipe.Create("T", "D", 0, 0, 1, bad, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*At least one ingredient is required*");
        }

        [Theory]
        [MemberData(nameof(InvalidListSets))]
        public void Create_InvalidInstructions_Throws_ArgumentException(IEnumerable<string> bad)
        {
            var action = () => { Recipe.Create("T", "D", 0, 0, 1, new[] { "i" }, bad); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*At least one instruction is required*");
        }

        [Fact]
        public void Update_WithValidArguments_ReplacesAllFields()
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var oldId = recipe.Id;
            var titleExpected = "t2";
            var descriptionExpected = "d2";
            var prepTimeExpected = 2;
            var cookTimeExpected = 2;
            var servingsExpected = 2;
            var ingredientesExpected = new List<string> { "ing2" };
            var instructionsExpected = new List<string> { "inst2" };

            recipe.Update(titleExpected, descriptionExpected, prepTimeExpected, cookTimeExpected, servingsExpected,
                           ingredientesExpected, instructionsExpected);

            recipe.Id.Should().Be(oldId);
            recipe.Title.Should().Be(titleExpected);
            recipe.Description.Should().Be(descriptionExpected);
            recipe.PreparationTime.Should().Be(prepTimeExpected);
            recipe.CookingTime.Should().Be(cookTimeExpected);
            recipe.Servings.Should().Be(servingsExpected);
            recipe.Ingredients.Should().Equal(ingredientesExpected);
            recipe.Instructions.Should().Equal(instructionsExpected);
        }

#pragma warning disable xUnit1012
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
#pragma warning restore xUnit1012 
        public void Update_InvalidTitle_Throws_ArgumentException(string bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update(bad, "D", 0, 0, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Title is required*");
        }


#pragma warning disable xUnit1012
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
#pragma warning restore xUnit1012 
        public void Update_InvalidDescription_Throws_ArgumentException(string bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update("T", bad, 0, 0, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Description is required*");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Update_InvalidPreparationTime_Throws_ArgumentException(int bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update("T", "D", bad, 0, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Preparation time must be at least 0*");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Update_InvalidCookingTime_Throws_ArgumentException(int bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update("T", "D", 0, bad, 1, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Cooking time must be at least 0*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Update_InvalidServings_Throws_ArgumentException(int bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update("T", "D", 0, 0, bad, new[] { "i" }, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Servings must be superior to 0*");
        }

        [Theory]
        [MemberData(nameof(InvalidListSets))]
        public void Update_InvalidIngredients_Throws_ArgumentException(IEnumerable<string> bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update("T", "D", 0, 0, 1, bad, new[] { "i" }); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*At least one ingredient is required*");
        }

        [Theory]
        [MemberData(nameof(InvalidListSets))]
        public void Update_InvalidInstructions_Throws_ArgumentException(IEnumerable<string> bad)
        {
            var recipe = Recipe.Create(
                title: "t1",
                description: "d1",
                preparationTime: 1,
                cookingTime: 1,
                servings: 1,
                ingredients: new List<string> { "ing1" },
                instructions: new List<string> { "inst1" }
            );

            var action = () => { recipe.Update("T", "D", 0, 0, 1, new[] { "i" }, bad); };

            action.Should().Throw<ArgumentException>()
                .WithMessage("*At least one instruction is required*");
        }

        [Fact]
        public void Update_IsDefensive_Copy_OnLists()
        {
            var recipe = Recipe.Create(
                            title: "t1",
                            description: "d1",
                            preparationTime: 1,
                            cookingTime: 1,
                            servings: 1,
                            ingredients: new List<string> { "ing1" },
                            instructions: new List<string> { "inst1" }
                        );

            var ingr = new List<string> { "x" };
            var instr = new List<string> { "y" };

            recipe.Update("t2", "d2", 2, 2, 3, ingr, instr);

            ingr.Add("z");
            instr.Add("z");

            recipe.Ingredients.Should().NotContain("z");
            recipe.Instructions.Should().NotContain("z");
        }
    }
}