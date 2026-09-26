// src/types/recipe.ts
// Aliases over the generated contract (R-09 / ADR-019). Never add fields here: change the C# DTO, then
// regenerate (see README, "Changing the API contract").
import type { components } from './generated/api';

type Schemas = components['schemas'];

export type Recipe = Schemas['RecipeDto'];
export type CreateRecipeRequest = Schemas['CreateRecipeCommand'];
export type UpdateRecipeRequest = Schemas['UpdateRecipeDto'];
export type Ingredient = Schemas['IngredientDto'];
export type IngredientInput = Schemas['IngredientInputDto'];
export type InstructionStep = Schemas['InstructionStepDto'];
export type InstructionStepInput = Schemas['InstructionStepInputDto'];
