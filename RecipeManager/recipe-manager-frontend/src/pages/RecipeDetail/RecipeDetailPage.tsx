// src/pages/RecipeDetail/RecipeDetailPage.tsx

import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { IngredientRail, MethodSteps, RecipeDetailTabs } from '@/components';
import { PrintIcon } from '@/components/ui/Icon';
import { isRecipeId, useMediaQuery, useRecipe } from '@/hooks';
import { isNotFoundError } from '@/services';
import type { Recipe } from '@/types';
import { formatDuration, getISODuration } from '@/utils/duration';
import styles from './RecipeDetailPage.module.css';

// Same value as every `@media (max-width: 768px)` in the app, so the tabs appear exactly when the
// BottomNav does.
const MOBILE = '(max-width: 768px)';

const BackLink = () => (
  <Link to="/recipes" className={styles.back}>← All recipes</Link>
);

const Duration = ({ minutes }: { minutes: number }) => (
  <time dateTime={getISODuration(minutes)}>{formatDuration(minutes)}</time>
);

const RecipeDetail = ({ recipe }: { recipe: Recipe }) => {
  // Local state: servings describes this visit, not the recipe. Never persisted, never sent.
  const [servings, setServings] = useState(recipe.servings);
  const isMobile = useMediaQuery(MOBILE);

  const rail = (
    <IngredientRail
      ingredients={recipe.ingredients}
      writtenServings={recipe.servings}
      servings={servings}
      onServingsChange={setServings}
    />
  );
  const method = <MethodSteps steps={recipe.instructions} />;

  return (
    <article className={styles.page}>
      <BackLink />

      <header className={styles.header}>
        <h1 className={styles.title}>{recipe.title}</h1>
        <p className={styles.description}>{recipe.description}</p>

        <dl className={styles.stats}>
          <div>
            <dt className={styles.statLabel}>Total</dt>
            <dd className={styles.statValue}><Duration minutes={recipe.preparationTime + recipe.cookingTime} /></dd>
          </div>
          {recipe.preparationTime > 0 && (
            <div>
              <dt className={styles.statLabel}>Hands on</dt>
              <dd className={styles.statValue}><Duration minutes={recipe.preparationTime} /></dd>
            </div>
          )}
          {recipe.cookingTime > 0 && (
            <div>
              <dt className={styles.statLabel}>Cooking</dt>
              <dd className={styles.statValue}><Duration minutes={recipe.cookingTime} /></dd>
            </div>
          )}
          <div>
            <dt className={styles.statLabel}>Serves</dt>
            {/* Follows the stepper, so the page never shows two different servings counts. */}
            <dd className={styles.statValue} data-testid="serves">{servings}</dd>
          </div>
        </dl>
      </header>

      {isMobile ? (
        <RecipeDetailTabs ingredients={rail} method={method} />
      ) : (
        <div className={styles.columns}>
          <section className={styles.method} aria-labelledby="method-heading">
            <h2 id="method-heading" className={styles.sectionHeading}>Method</h2>
            {method}
          </section>
          {rail}
        </div>
      )}

      <button type="button" className={styles.print} onClick={() => window.print()}>
        <PrintIcon className={styles.printIcon} />
        Print
      </button>
    </article>
  );
};

const RecipeNotFound = () => (
  <section className={styles.message}>
    <h1 className={styles.messageTitle}>Recipe not found</h1>
    <p className={styles.description}>It may have been deleted, or the link is wrong.</p>
    <BackLink />
  </section>
);

export const RecipeDetailPage: React.FC = () => {
  const { id = '' } = useParams();
  const query = useRecipe(id);

  // Checked before isPending: a disabled query stays pending forever and would read "Loading recipe…".
  if (!isRecipeId(id)) return <RecipeNotFound />;

  if (query.isPending) {
    return (
      <p className={styles.status}>
        {query.fetchStatus === 'paused'
          ? 'You appear to be offline. The recipe will load when you reconnect.'
          : 'Loading recipe…'}
      </p>
    );
  }

  if (query.isError) {
    if (isNotFoundError(query.error)) return <RecipeNotFound />;
    // The error's own message is deliberately not rendered: it can carry server detail (SEC-05).
    return (
      <section className={styles.message}>
        <p className={styles.description}>Couldn't load this recipe.</p>
        <button type="button" className={styles.retry} onClick={() => void query.refetch()}>
          Retry
        </button>
      </section>
    );
  }

  // Keyed by id: opening another recipe starts a fresh RecipeDetail, so servings resets to that
  // recipe's own count instead of carrying the previous one over.
  return <RecipeDetail key={query.data.id} recipe={query.data} />;
};

export default RecipeDetailPage;
