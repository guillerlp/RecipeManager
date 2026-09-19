// src/types/recipe.ts
// Mirrors RecipeDto / UpdateRecipeDto / CreateRecipeCommand field for field (docs/agents/08-api-contract.md).

export interface Recipe {
  id: string;                 // Guid, serialised as a JSON string
  title: string;
  description: string;
  preparationTime: number;    // minutes
  cookingTime: number;        // minutes
  servings: number;
  ingredients: string[];
  instructions: string[];
}

// POST body. The server generates the id.
export type CreateRecipeRequest = Omit<Recipe, 'id'>;

// PUT body. The id travels in the route, and every field is required: the endpoint binds a full UpdateRecipeDto.
export type UpdateRecipeRequest = Omit<Recipe, 'id'>;
