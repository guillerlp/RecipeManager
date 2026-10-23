// src/pages/CookingMode/CookingModePage.tsx
// Cooking mode (spec 017, ADR-029): one step at a time, read at arm's length. Outside the app chrome, under the
// cooking palette in both themes.
import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowBackIcon, ArrowForwardIcon, CloseIcon } from '@/components/ui/Icon';
import { isRecipeId, useRecipe, useUnits } from '@/hooks';
import { isNotFoundError } from '@/services';
import type { Recipe } from '@/types';
import { ingredientLists, parseServings, parseStep } from './cooking';
import { IngredientLists } from './IngredientLists';
import styles from './CookingModePage.module.css';

// Every state paints the cooking palette, so loading never flashes the paper theme first.
const Frame = ({ children }: { children: React.ReactNode }) => (
  <main data-palette="cooking" className={styles.page}>{children}</main>
);

const Message = ({ text, children }: { text: string; children?: React.ReactNode }) => (
  <Frame>
    <section className={styles.message}>
      <p className={styles.messageText}>{text}</p>
      {children}
    </section>
  </Frame>
);

// Not Detail's RecipeNotFound: that one leans on the app chrome around it, and there is none here.
const NotFound = () => (
  <Message text="We couldn't find that recipe.">
    <Link to="/recipes" className={styles.messageLink}>All recipes</Link>
  </Message>
);

const Cooking = ({ recipe }: { recipe: Recipe }) => {
  const [params, setParams] = useSearchParams();
  const { unitSystem } = useUnits();
  const [finishing, setFinishing] = useState(false);
  const stepCount = recipe.instructions.length;
  const step = parseStep(params.get('step'), stepCount);
  const writtenServings = recipe.servings ?? null;
  const servings = parseServings(params.get('servings'), writtenServings);
  const isLast = step === stepCount;

  // replace, not push: the browser's Back leaves cooking mode instead of walking back through every step.
  const goTo = useCallback((next: number) => {
    setParams(current => {
      const updated = new URLSearchParams(current);
      updated.set('step', String(next));
      return updated;
    }, { replace: true });
  }, [setParams]);

  // Focus follows the step so a screen reader announces it; skipped on mount, where focus starts at the top.
  const headingRef = useRef<HTMLHeadingElement>(null);
  const mounted = useRef(false);
  useEffect(() => {
    if (mounted.current) headingRef.current?.focus();
    mounted.current = true;
  }, [step]);

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (finishing || event.altKey || event.ctrlKey || event.metaKey) return;
      if (event.key === 'ArrowRight' && !isLast) goTo(step + 1);
      if (event.key === 'ArrowLeft' && step > 1) goTo(step - 1);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [finishing, isLast, step, goTo]);

  return (
    <Frame>
      <header className={styles.topBar}>
        <Link to={`/recipes/${recipe.id}`} replace aria-label="Leave cooking mode" className={styles.close}>
          <CloseIcon />
        </Link>
        <h1 className={styles.title}>{recipe.title}</h1>
      </header>

      <div className={styles.layout}>
        <section className={styles.step} aria-labelledby="cooking-step">
          <h2 id="cooking-step" ref={headingRef} tabIndex={-1} className={styles.stepLabel}>
            Step {step} of {stepCount}
          </h2>
          <p className={styles.stepText}>{recipe.instructions[step - 1].text}</p>
        </section>

        <aside className={styles.ingredients} aria-label="Ingredients for this recipe">
          <IngredientLists
            lists={ingredientLists(recipe, step - 1)}
            writtenServings={writtenServings}
            servings={servings}
            unitSystem={unitSystem}
          />
        </aside>
      </div>

      <div className={styles.controls}>
        {/* aria-disabled, not disabled: a disabled button drops focus out of the page (the R-21 reorder rule). */}
        <button
          type="button"
          className={styles.back}
          aria-disabled={step === 1}
          onClick={() => step > 1 && goTo(step - 1)}
        >
          <ArrowBackIcon /> Back
        </button>
        {isLast ? (
          <button type="button" className={styles.next} onClick={() => setFinishing(true)}>Finish</button>
        ) : (
          <button type="button" className={styles.next} onClick={() => goTo(step + 1)}>
            Next step <ArrowForwardIcon />
          </button>
        )}
      </div>
    </Frame>
  );
};

export const CookingModePage = () => {
  const { id = '' } = useParams();
  const query = useRecipe(id);

  // Checked before isPending: a disabled query stays pending forever.
  if (!isRecipeId(id)) return <NotFound />;

  if (query.isPending) {
    return (
      <Message
        text={query.fetchStatus === 'paused'
          ? 'You appear to be offline. The recipe will load when you reconnect.'
          : 'Loading recipe…'}
      />
    );
  }

  if (query.isError) {
    if (isNotFoundError(query.error)) return <NotFound />;
    // The error's own message is deliberately not rendered: it can carry server detail (SEC-05).
    return (
      <Message text="Couldn't load this recipe.">
        <button type="button" className={styles.messageButton} onClick={() => void query.refetch()}>Retry</button>
      </Message>
    );
  }

  if (query.data.instructions.length === 0) {
    return (
      <Message text="This recipe has no steps yet.">
        <Link to={`/recipes/${id}`} className={styles.messageLink}>Back to the recipe</Link>
      </Message>
    );
  }

  return <Cooking key={query.data.id} recipe={query.data} />;
};
