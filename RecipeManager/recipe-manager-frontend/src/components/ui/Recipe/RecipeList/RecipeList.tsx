// src/pages/RecipeList.tsx
import React, { useMemo } from 'react';
import { RecipeCard } from '@/components';
import styles from './RecipeList.module.css';
import { useRecipes } from '@/hooks/useRecipes';
import type { RecipeStatus } from '@/types';

interface RecipeListProps {
  searchQuery? : string;
  /** Already normalised by the caller (RecipePage), so an exact match is correct. */
  tag?: string;
  /** Which list to show (ADR-025). */
  status?: RecipeStatus;
}

export const RecipeList: React.FC<RecipeListProps> = ({searchQuery = '', tag, status = 'Published'}) => {

  const {
      data: recipes = [],
      isLoading: loading,
      error,
      refetch,
  } = useRecipes(status);

  // Derived during render, never stored: the tag narrows first, then the text search (AND), and tags are
  // searchable text too (spec 014).
  const filteredRecipes = useMemo(() => {
    const query = searchQuery.toLowerCase().trim();

    return recipes.filter(recipe => {
      if (tag && !recipe.tags.includes(tag)) return false;
      if (!query) return true;

      const searchableText = [
        recipe.title,
        recipe.description,
        ...recipe.ingredients.map(i => i.name),
        ...recipe.tags,
      ].join(' ').toLowerCase();

      return searchableText.includes(query);
    });
  }, [recipes, searchQuery, tag])

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
          { searchQuery || tag ?
          (
            <>
              <h3 className={styles.emptyTitle}>No recipes found</h3>
              <p className={styles.emptyBody}>
                {tag
                  ? `No recipes tagged "${tag}"${searchQuery ? ` match "${searchQuery}"` : ''}.`
                  : `No recipes match "${searchQuery}". Try a different search term.`}
              </p>
            </>
          ) :
          status === 'Draft' ? (
            <>
              <h3 className={styles.emptyTitle}>No drafts</h3>
              <p className={styles.emptyBody}>Save a recipe as a draft and it'll wait here.</p>
            </>
          ) : (
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
