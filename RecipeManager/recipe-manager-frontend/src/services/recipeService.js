import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const recipeService = {
    getAllRecipes: () => api.get('/Recipes'),
    getRecipeById: (id) => api.get(`/Recipes/${id}`),
    createRecipe: (recipe) => api.post('/Recipes', recipe),
    updateRecipe: (id, recipe) => api.put(`/Recipes/${id}`, recipe),
    deleteRecipe: (id) => api.delete(`/Recipes/${id}`),
}