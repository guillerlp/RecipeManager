// src/pages/RecipeList.tsx
import React, { useState, useEffect } from 'react';
import type { Recipe } from '@/types';
import { recipeService } from '@/services';
import { RecipeCard } from '@/components';
import styles from './RecipeList.module.css';

export const RecipeList: React.FC = () => {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRecipes = async (): Promise<void> => {
      setLoading(true);
      try {
        const { data } = await recipeService.getAllRecipes();
        setRecipes(data);
      } catch (err) {
        console.error('Error fetching recipes:', err);
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchRecipes();
  }, []);

  const handleRecipeClick = (recipe: Recipe) => {
    // Navigate to recipe detail page
    // Example: navigate(`/recipes/${recipe.id}`);
    console.log('Clicked recipe:', recipe.title);
  };

  if (loading) return <div>Loading…</div>;
  if (error)   return <div>Error: {error}</div>;

  return (
    <section className={styles.heroSection}>
      {recipes.map((recipe) => (
        <RecipeCard
          key={recipe.id}
          recipe={recipe}
          onClick={handleRecipeClick}
        />
      ))}
    </section>
  );
};