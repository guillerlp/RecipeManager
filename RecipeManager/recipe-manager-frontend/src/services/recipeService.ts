// src/services/recipeService.ts
/// <reference types="vite/client" />

import type { CreateRecipeRequest, Recipe, UpdateRecipeRequest } from '@/types';
import axios, { type AxiosInstance, type AxiosResponse } from 'axios';

const rawBase = import.meta.env.VITE_API_URL as string | undefined;
const API_BASE_URL = rawBase ? rawBase.replace(/\/+$/, '') : '/api';


const api: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const recipeService = {
  getAllRecipes: (): Promise<AxiosResponse<Recipe[]>> =>
    api.get<Recipe[]>('/Recipes'),

  getRecipeById: (id: string): Promise<AxiosResponse<Recipe>> =>
    api.get<Recipe>(`/Recipes/${encodeURIComponent(id)}`),

  createRecipe: (recipe: CreateRecipeRequest): Promise<AxiosResponse<Recipe>> =>
    api.post<Recipe>('/Recipes', recipe),

  // PUT returns 204 No Content, so there is no body to type.
  updateRecipe: (id: string, recipe: UpdateRecipeRequest): Promise<AxiosResponse<void>> =>
    api.put<void>(`/Recipes/${encodeURIComponent(id)}`, recipe),

  deleteRecipe: (id: string): Promise<AxiosResponse<void>> =>
    api.delete<void>(`/Recipes/${encodeURIComponent(id)}`),
};

// Kept here so hooks and components can tell a missing recipe from a failure without importing axios.
export const isNotFoundError = (error: unknown): boolean =>
  axios.isAxiosError(error) && error.response?.status === 404;
