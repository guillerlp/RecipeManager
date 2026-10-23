// src/pages/Home/HomePage.tsx

import { Link } from 'react-router-dom';
import { AddIcon, ArrowForwardIcon } from '@/components/ui/Icon';
import { useRecipes } from '@/hooks/useRecipes';
import { summariseCooks } from '@/utils/cookStats';
import { formatRelativeDate } from '@/utils/relativeDate';
import styles from './HomePage.module.css';

export const HomePage: React.FC = () => {
  const { data: recipes, isLoading, error } = useRecipes();
  // Drafts count as cooks too: otherwise "78 cooks" would disagree with the recipes' own "Cooked N times".
  const { data: drafts } = useRecipes('Draft');

  // A pending or failed count must not hide the rest of the page — only render it once the
  // query has resolved without error.
  const showCount = !isLoading && !error && recipes !== undefined;
  // Waits for both lists, so the number never jumps once the drafts arrive.
  const { totalCooks, recentlyCooked } =
    showCount && drafts !== undefined ? summariseCooks([...recipes, ...drafts]) : summariseCooks([]);
  const now = new Date();

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        {showCount && (
          <p className={styles.count}>
            {recipes.length} {recipes.length === 1 ? 'recipe' : 'recipes'}
            {totalCooks > 0 && ` · ${totalCooks} ${totalCooks === 1 ? 'cook' : 'cooks'}`} · yours alone
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

      {/* No empty state in the design: an empty box is noise, so the rail appears with the first cook. */}
      {recentlyCooked.length > 0 && (
        <aside className={styles.rail} aria-labelledby="last-cooked-heading">
          <h2 id="last-cooked-heading" className={styles.railHeading}>Last cooked</h2>
          <ul className={styles.railList}>
            {recentlyCooked.map(recipe => (
              <li key={recipe.id} className={styles.railItem}>
                <Link to={`/recipes/${recipe.id}`} className={styles.railLink}>{recipe.title}</Link>
                {recipe.lastCookedAt && (
                  <time dateTime={recipe.lastCookedAt} className={styles.railWhen}>
                    {formatRelativeDate(new Date(recipe.lastCookedAt), now)}
                  </time>
                )}
              </li>
            ))}
          </ul>
        </aside>
      )}
    </div>
  );
};
