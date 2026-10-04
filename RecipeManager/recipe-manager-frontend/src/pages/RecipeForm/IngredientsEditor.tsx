import { useEffect, useState, type Dispatch } from 'react';
import { describeIngredient } from '@/utils/ingredientLine';
import { FIELD_IDS, emptyIngredient, parseRow, type FormAction, type IngredientRow } from './recipeForm';
import styles from './RecipeFormPage.module.css';

interface IngredientsEditorProps {
  rows: IngredientRow[];
  dispatch: Dispatch<FormAction>;
  error: string[] | undefined;
  announce: (message: string) => void;
}

// One text input per ingredient, parsed as it is typed (spec 015 §8.2). Each row keeps its key and id, so editing a
// line never breaks the steps that use it — which a single re-parsed textarea would.
export const IngredientsEditor = ({ rows, dispatch, error, announce }: IngredientsEditorProps) => {
  // The row to focus when it mounts; autoFocus only fires on mount, so existing rows are unaffected.
  const [focusKey, setFocusKey] = useState<string | null>(null);

  // Where focus goes after a remove or a move. A removed row's × unmounts with it, and focus would fall to <body>
  // (WCAG 2.4.3); a moved row is re-ordered in the DOM, which can drop focus too. A new object each time, so a
  // repeat towards the same target still refocuses.
  const [refocus, setRefocus] = useState<{ id: string } | null>(null);
  useEffect(() => {
    if (refocus) document.getElementById(refocus.id)?.focus();
  }, [refocus]);

  const removeRow = (index: number) => {
    const neighbour = rows[index + 1] ?? rows[index - 1];
    setRefocus({ id: neighbour ? `ingredient-${neighbour.key}` : 'add-ingredient' });
    dispatch({ type: 'removeIngredient', key: rows[index].key });
  };

  // aria-disabled, not disabled, at the ends: a disabled button drops focus to <body>, stranding a keyboard user.
  const moveRow = (index: number, by: -1 | 1, label: string) => {
    const to = index + by;
    if (to < 0 || to >= rows.length) return;
    const { key } = rows[index];
    dispatch({ type: 'moveIngredient', key, by });
    setRefocus({ id: `move-${by === -1 ? 'up' : 'down'}-${key}` });
    announce(`Moved ${label} to position ${to + 1} of ${rows.length}.`);
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
                id={`move-up-${row.key}`}
                type="button"
                className={styles.remove}
                aria-label={`Move ${label} up`}
                aria-disabled={index === 0 ? true : undefined}
                onClick={() => moveRow(index, -1, label)}
              >
                ↑
              </button>
              <button
                id={`move-down-${row.key}`}
                type="button"
                className={styles.remove}
                aria-label={`Move ${label} down`}
                aria-disabled={index === rows.length - 1 ? true : undefined}
                onClick={() => moveRow(index, 1, label)}
              >
                ↓
              </button>
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
