import { useEffect, useState, type Dispatch } from 'react';
import { describeIngredient } from '@/utils/ingredientLine';
import { FIELD_IDS, emptyIngredient, parseRow, type FormAction, type IngredientRow } from './recipeForm';
import styles from './RecipeFormPage.module.css';

interface IngredientsEditorProps {
  rows: IngredientRow[];
  dispatch: Dispatch<FormAction>;
  error: string[] | undefined;
}

// One text input per ingredient, parsed as it is typed (spec 015 §8.2). Each row keeps its key and id, so editing a
// line never breaks the steps that use it — which a single re-parsed textarea would.
export const IngredientsEditor = ({ rows, dispatch, error }: IngredientsEditorProps) => {
  // The row to focus when it mounts; autoFocus only fires on mount, so existing rows are unaffected.
  const [focusKey, setFocusKey] = useState<string | null>(null);

  // Where focus goes after a remove: the removed row's × unmounts with it, and focus would fall to <body> (WCAG
  // 2.4.3). A new object each time, so removing twice towards the same target still refocuses.
  const [refocus, setRefocus] = useState<{ id: string } | null>(null);
  useEffect(() => {
    if (refocus) document.getElementById(refocus.id)?.focus();
  }, [refocus]);

  const removeRow = (index: number) => {
    const neighbour = rows[index + 1] ?? rows[index - 1];
    setRefocus({ id: neighbour ? `ingredient-${neighbour.key}` : 'add-ingredient' });
    dispatch({ type: 'removeIngredient', key: rows[index].key });
  };

  const add = (after?: string) => {
    const row = emptyIngredient();
    setFocusKey(row.key);
    dispatch({ type: 'addIngredient', row, after });
  };

  return (
    <section id={FIELD_IDS.ingredients} tabIndex={-1} className={styles.section} aria-labelledby="ingredients-heading">
      <div className={styles.sectionHead}>
        <h2 id="ingredients-heading" className={styles.sectionHeading}>Ingredients</h2>
        <span className={styles.limit}>Up to 50</span>
      </div>
      <p id="ingredients-hint" className={styles.hint}>One per line, the way you'd say it — 2 tbsp butter, cold</p>
      {error && <p className={styles.error}>{error.join(' ')}</p>}

      <ul role="list" className={styles.rows}>
        {rows.map((row, index) => {
          const parsed = parseRow(row);
          const messageId = `ingredient-${row.key}-message`;
          const label = parsed?.ok ? parsed.value.name : `ingredient ${index + 1}`;
          return (
            <li key={row.key} className={styles.row}>
              <input
                id={`ingredient-${row.key}`}
                className={styles.line}
                value={row.text}
                autoFocus={row.key === focusKey}
                aria-label={`Ingredient ${index + 1}`}
                aria-invalid={parsed?.ok === false ? true : undefined}
                aria-describedby={parsed ? messageId : 'ingredients-hint'}
                onChange={event => dispatch({ type: 'setIngredient', key: row.key, text: event.target.value })}
                onKeyDown={event => {
                  if (event.key !== 'Enter') return;
                  event.preventDefault();
                  add(row.key);
                }}
              />
              <button
                type="button"
                className={styles.remove}
                aria-label={`Remove ${label}`}
                onClick={() => removeRow(index)}
              >
                ×
              </button>
              {parsed && (
                <p id={messageId} className={parsed.ok ? styles.preview : styles.rowError}>
                  {parsed.ok ? `→ ${describeIngredient(parsed.value)}` : parsed.error}
                </p>
              )}
            </li>
          );
        })}
      </ul>

      <button id="add-ingredient" type="button" className={styles.add} onClick={() => add()}>Add ingredient</button>
      <p className={styles.footnote}>Stored as quantity · unit · name, so the recipe page can rescale them.</p>
    </section>
  );
};
