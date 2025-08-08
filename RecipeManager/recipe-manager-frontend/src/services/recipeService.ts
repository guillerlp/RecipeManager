// src/services/recipeService.ts
/// <reference types="vite/client" />

import axios, { type AxiosInstance, type AxiosResponse } from 'axios';
import type { Recipe } from '../types/recipe.js';

const API_BASE_URL: string = import.meta.env.VITE_API_URL ?? '/api';

const api: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const recipeService = {
  getAllRecipes: (): Promise<AxiosResponse<Recipe[]>> =>
    api.get<Recipe[]>('/Recipes'),

  getRecipeById: (id: number): Promise<AxiosResponse<Recipe>> =>
    api.get<Recipe>(`/Recipes/${id}`),

  createRecipe: (
    recipe: Omit<Recipe, 'id'>
  ): Promise<AxiosResponse<Recipe>> =>
    api.post<Recipe>('/Recipes', recipe),

  updateRecipe: (
    id: number,
    recipe: Partial<Omit<Recipe, 'id'>>
  ): Promise<AxiosResponse<Recipe>> =>
    api.put<Recipe>(`/Recipes/${id}`, recipe),

  deleteRecipe: (id: number): Promise<AxiosResponse<void>> =>
    api.delete<void>(`/Recipes/${id}`),
};