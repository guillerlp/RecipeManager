// src/pages/RecipeList.tsx
import React, { useMemo } from 'react';
import type { Recipe } from '@/types';
import { RecipeCard } from '@/components';
import styles from './RecipeList.module.css';
import { useRecipes } from '@/hooks/useRecipes';

interface RecipeListProps {
  searchQuery? : string;
}

export const RecipeList: React.FC<RecipeListProps> = ({searchQuery = ''}) => {

  const {
      data: recipes = [],
      isLoading: loading,
      error,
  } = useRecipes();

  const filteredRecipes = useMemo(() => {

    if(!searchQuery.trim()){
      return recipes;
    }

    const query = searchQuery.toLowerCase().trim();

    return recipes.filter(recipe => {
      const searchableText = [
        recipe.title,
        recipe.description,
        ...recipe.ingredients
      ].join(' ').toLowerCase();

      return searchableText.includes(query);
    });
  }, [recipes, searchQuery])

  const handleRecipeClick = (recipe: Recipe) => {
    console.log('Clicked recipe:', recipe.title);
  };

  if (loading) return <div className={styles.loadingSection}>Loading…</div>;
  if (error) {
    return (
      <div className={styles.errorSection}>
        Error: {error instanceof Error ? error.message : 'Unknown error'}
        <button onClick={() => window.location.reload()}>
          Retry
        </button>
      </div>
    );
  }

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