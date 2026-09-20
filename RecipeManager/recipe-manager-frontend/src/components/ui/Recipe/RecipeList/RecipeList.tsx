// src/pages/RecipeList.tsx
import React, { useMemo } from 'react';
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
      refetch,
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

  if (loading) return <p className={styles.loadingSection}>Loading…</p>;
  if (error) {
    return (
      <div className={styles.errorSection}>
        Error: {error instanceof Error ? error.message : 'Unknown error'}
        <button onClick={() => void refetch()}>
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
              <h3 className={styles.emptyTitle}>No recipes found</h3>
              <p className={styles.emptyBody}>No recipes match "{searchQuery}". Try a different search term.</p>
            </>
          ) :
          (
            <>
              <h3 className={styles.emptyTitle}>No recipes available</h3>
              <p className={styles.emptyBody}>Start by adding some recipes to your collection.</p>
            </>
          )}
        </div>
      ) :
      (
        <>
          {searchQuery && (
            <div>
              Found {filteredRecipes.length} recipe{filteredRecipes.length !== 1 ? 's' : ''}
              {searchQuery && ` matching "${searchQuery}"`}
            </div>
          )}

          <ul className={styles.list}>
            {filteredRecipes.map((recipe) => (
              <li key={recipe.id}>
                <RecipeCard recipe={recipe} />
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
};
