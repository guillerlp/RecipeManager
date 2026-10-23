// src/pages/CookingMode/CookingModePage.tsx
// Cooking mode (spec 017, ADR-029): one step at a time, read at arm's length. Outside the app chrome, under the
// cooking palette in both themes.
import { Link, useParams } from 'react-router-dom';
import { isRecipeId, useRecipe } from '@/hooks';
import { isNotFoundError } from '@/services';
import type { Recipe } from '@/types';
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

const Cooking = ({ recipe }: { recipe: Recipe }) => (
  <Frame>
    <h1 className={styles.title}>{recipe.title}</h1>
  </Frame>
);

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
