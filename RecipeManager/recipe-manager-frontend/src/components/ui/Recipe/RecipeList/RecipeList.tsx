// src/pages/RecipeList.tsx
import React, { useState, useEffect, useMemo } from 'react';
import type { Recipe } from '@/types';
import { recipeService } from '@/services';
import { RecipeCard } from '@/components';
import styles from './RecipeList.module.css';

interface RecipeListProps {
  searchQuery? : string;
}

export const RecipeList: React.FC<RecipeListProps> = ({searchQuery = ''}) => {
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

  const filteredRecipes = useMemo(() => {

    if(!searchQuery.trim()){
      return recipes;
    }

    const query = searchQuery.toLowerCase().trim();

    return recipes.filter( recipe => {
      
        if(recipe.title.toLowerCase().includes(query)) {
          return true;
        }

        if(recipe.description.toLowerCase().includes(query)) {
          return true;
        }

        if(recipe.ingredients.some(
            ingredient => ingredient.toLowerCase().includes(query)
          )) 
        {
          return true;
        }

        return false;
    })
  }, [recipes, searchQuery])

  const handleRecipeClick = (recipe: Recipe) => {
    // Navigate to recipe detail page
    // Example: navigate(`/recipes/${recipe.id}`);
    console.log('Clicked recipe:', recipe.title);
  };

  if (loading) return <div>Loading…</div>;
  if (error)   return <div>Error: {error}</div>;

  return (
    <section className={styles.heroSection}>
      {filteredRecipes.length === 0 ? 
      (
        <div>
          { searchQuery ? 
          (
            <>
              <h3>No recipes found</h3>
              <p>No recipes match "{searchQuery}". Try a different search term.</p>
            </>
          ) : 
          (
            <>
              <h3>No recipes available</h3>
              <p>Start by adding some recipes to your collection.</p>
            </>
          )}
        </div>
      ) : 
      (
        <>
          {searchQuery && (
            <div className={styles.resultsCount}>
              Found {filteredRecipes.length} recipe{filteredRecipes.length !== 1 ? 's' : ''} 
              {searchQuery && ` matching "${searchQuery}"`}
            </div>
          )}

          {filteredRecipes.map((recipe) => (
            <RecipeCard
              key={recipe.id}
              recipe={recipe}
              onClick={handleRecipeClick}
            />
          ))}
        </>
      )}
    </section>
  );
};