// src/pages/RecipeDetail/RecipeDetailPage.tsx

import { Fragment, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { IngredientRail, MethodSteps, RecipeDetailTabs } from '@/components';
import { PrintIcon } from '@/components/ui/Icon';
import { isRecipeId, useMediaQuery, useRecipe, useUnits } from '@/hooks';
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
  // A draft may have no servings, times or description yet (spec 013): an absent value renders nothing,
  // not "0". The generated type allows undefined as well as null, so normalise once here.
  const writtenServings = recipe.servings ?? null;
  const [servings, setServings] = useState(writtenServings);
  const prep = recipe.preparationTime ?? 0;
  const cook = recipe.cookingTime ?? 0;
  const isMobile = useMediaQuery(MOBILE);
  const { unitSystem } = useUnits();

  const rail = (
    <IngredientRail
      ingredients={recipe.ingredients}
      writtenServings={writtenServings}
      servings={servings}
      onServingsChange={setServings}
      unitSystem={unitSystem}
    />
  );
  const method = <MethodSteps steps={recipe.instructions} />;

  return (
    <article className={styles.page}>
      {/* A link, not a button: Edit goes somewhere (ADR-024), so it can be opened in a new tab. */}
      <div className={styles.topBar}>
        <BackLink />
        <Link to={`/recipes/${recipe.id}/edit`} className={styles.edit}>Edit</Link>
      </div>

      <header className={styles.header}>
        {recipe.tags.length > 0 && (
          <p className={styles.kicker}>
            {recipe.tags.map((tag, index) => (
              <Fragment key={tag}>
                {index > 0 && <span aria-hidden="true"> · </span>}
                {/* Encoded: a tag is user text, and "a&b" or "x/y" must not become a different query or route. */}
                <Link to={`/recipes?tag=${encodeURIComponent(tag)}`}>{tag}</Link>
              </Fragment>
            ))}
          </p>
        )}
        {recipe.status === 'Draft' && <p className={styles.statLabel}>Draft</p>}
        <h1 className={styles.title}>{recipe.title}</h1>
        {recipe.description && <p className={styles.description}>{recipe.description}</p>}

        <dl className={styles.stats}>
          {prep + cook > 0 && (
            <div>
              <dt className={styles.statLabel}>Total</dt>
              <dd className={styles.statValue}><Duration minutes={prep + cook} /></dd>
            </div>
          )}
          {prep > 0 && (
            <div>
              <dt className={styles.statLabel}>Hands on</dt>
              <dd className={styles.statValue}><Duration minutes={prep} /></dd>
            </div>
          )}
          {cook > 0 && (
            <div>
              <dt className={styles.statLabel}>Cooking</dt>
              <dd className={styles.statValue}><Duration minutes={cook} /></dd>
            </div>
          )}
          {servings !== null && (
            <div>
              <dt className={styles.statLabel}>Serves</dt>
              {/* Follows the stepper, so the page never shows two different servings counts. */}
              <dd className={styles.statValue} data-testid="serves">{servings}</dd>
            </div>
          )}
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
