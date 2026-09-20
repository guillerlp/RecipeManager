// src/pages/Home/HomePage.tsx

import { Link } from 'react-router-dom';
import { AddIcon, ArrowForwardIcon } from '@/components/ui/Icon';
import { useRecipes } from '@/hooks/useRecipes';
import styles from './HomePage.module.css';

export const HomePage: React.FC = () => {
  const { data: recipes, isLoading, error } = useRecipes();

  // A pending or failed count must not hide the rest of the page — only render it once the
  // query has resolved without error.
  const showCount = !isLoading && !error && recipes !== undefined;

  return (
    <section className={styles.hero}>
      {showCount && (
        <p className={styles.count}>
          {recipes.length} {recipes.length === 1 ? 'recipe' : 'recipes'} · yours alone
        </p>
      )}
      <h1 className={styles.title}>Everything you actually cook, in one place.</h1>
      <p className={styles.lede}>
        No ads, no life story before the ingredients. Just your recipes, on your own server, ready
        to scale to however many people turned up.
      </p>
      <nav className={styles.actions} aria-label="Main recipe management actions">
        <Link to="/recipes" className={styles.primary}>
          Browse recipes
          <ArrowForwardIcon />
        </Link>
        <Link to="/recipes/new" className={styles.secondary}>
          <AddIcon />
          Add a recipe
        </Link>
      </nav>
    </section>
  );
};
