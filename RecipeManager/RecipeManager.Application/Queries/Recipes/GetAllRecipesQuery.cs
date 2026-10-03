using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Application.Queries.Recipes;

public record GetAllRecipesQuery(RecipeStatus Status = RecipeStatus.Published) : IQuery<IEnumerable<RecipeDto>>;
