// src/types/recipe.ts
export interface Recipe {
  id: number;
  title: string;
  description: string;
  preparationTime: number;
  cookingTime: number;
  ingredients: string[];

  image?: string;
}