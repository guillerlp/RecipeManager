import { useEffect, useState, type Dispatch } from 'react';
import { FIELD_IDS, emptyStep, parseRow, type FormAction, type IngredientRow, type StepRow } from './recipeForm';
import styles from './RecipeFormPage.module.css';

interface StepsEditorProps {
  steps: StepRow[];
  ingredients: IngredientRow[];
  dispatch: Dispatch<FormAction>;
  error: string[] | undefined;
  announce: (message: string) => void;
}

export const StepsEditor = ({ steps, ingredients, dispatch, error, announce }: StepsEditorProps) => {
  const [focusKey, setFocusKey] = useState<string | null>(null);
  // Only lines that name something can be picked: a blank or unparseable line has no name to show yet.
  const choices = ingredients.flatMap(row => {
    const parsed = parseRow(row);
    return parsed?.ok ? [{ key: row.key, name: parsed.value.name }] : [];
  });

  // Same as IngredientsEditor: after a remove focus moves to a neighbour or Add step; after a move it stays on the
  // moved step's control.
  const [refocus, setRefocus] = useState<{ id: string } | null>(null);
  useEffect(() => {
    if (refocus) document.getElementById(refocus.id)?.focus();
  }, [refocus]);

  const removeStep = (index: number) => {
    const neighbour = steps[index + 1] ?? steps[index - 1];
    setRefocus({ id: neighbour ? `step-${neighbour.key}` : 'add-step' });
    dispatch({ type: 'removeStep', key: steps[index].key });
  };

  const moveStep = (index: number, by: -1 | 1) => {
    const to = index + by;
    if (to < 0 || to >= steps.length) return;
    const { key } = steps[index];
    dispatch({ type: 'moveStep', key, by });
    setRefocus({ id: `move-${by === -1 ? 'up' : 'down'}-${key}` });
    announce(`Moved step ${index + 1} to position ${to + 1} of ${steps.length}.`);
  };

  const add = () => {
    const step = emptyStep();
    setFocusKey(step.key);
    dispatch({ type: 'addStep', step });
  };

  return (
    <section id={FIELD_IDS.instructions} tabIndex={-1} className={styles.section} aria-labelledby="method-heading">
      <div className={styles.sectionHead}>
        <h2 id="method-heading" className={styles.sectionHeading}>Method</h2>
        <span className={styles.limit}>Up to 50</span>
      </div>
      {error && <p className={styles.error}>{error.join(' ')}</p>}

      {/* An <ol>: the order is the recipe, and a screen reader announces "1 of 3". The visible number is
          decorative because the list already says it. */}
      <ol role="list" className={styles.steps}>
        {steps.map((step, index) => {
          const n = index + 1;
          return (
            <li key={step.key} className={styles.step}>
              <span className={styles.stepNumber} aria-hidden="true">{n}</span>
              <div className={styles.stepBody}>
                <textarea
                  id={`step-${step.key}`}
                  className={styles.stepText}
                  value={step.text}
                  rows={3}
                  maxLength={2000}
                  autoFocus={step.key === focusKey}
                  aria-label={`Step ${n}`}
                  onChange={event => dispatch({ type: 'setStep', key: step.key, text: event.target.value })}
                />
                <label className={styles.duration}>
                  Timer
                  <input
                    className={styles.number}
                    type="number"
                    inputMode="numeric"
                    min={1}
                    max={1439}
                    step={1}
                    value={step.duration}
                    aria-label={`Step ${n} timer in minutes`}
                    onChange={event => dispatch({ type: 'setStep', key: step.key, duration: event.target.value })}
                  />
                  minutes, optional
                </label>
                {choices.length > 0 && (
                  <fieldset className={styles.picker}>
                    <legend className={styles.pickerLegend}>Ingredients used in step {n}</legend>
                    {choices.map(choice => (
                      <label key={choice.key} className={styles.pick}>
                        <input
                          type="checkbox"
                          checked={step.ingredientKeys.includes(choice.key)}
                          onChange={() => dispatch({ type: 'toggleStepIngredient', stepKey: step.key, ingredientKey: choice.key })}
                        />
                        {choice.name}
                      </label>
                    ))}
                  </fieldset>
                )}
              </div>
              <div className={styles.stepControls}>
              <button
                id={`move-up-${step.key}`}
                type="button"
                className={styles.remove}
                aria-label={`Move step ${n} up`}
                aria-disabled={index === 0 ? true : undefined}
                onClick={() => moveStep(index, -1)}
              >
                ↑
              </button>
              <button
                id={`move-down-${step.key}`}
                type="button"
                className={styles.remove}
                aria-label={`Move step ${n} down`}
                aria-disabled={index === steps.length - 1 ? true : undefined}
                onClick={() => moveStep(index, 1)}
              >
                ↓
              </button>
              <button
                type="button"
                className={styles.remove}
                aria-label={`Remove step ${n}`}
                onClick={() => removeStep(index)}
              >
                ×
              </button>
              </div>
            </li>
          );
        })}
      </ol>

      <button id="add-step" type="button" className={styles.add} onClick={add}>Add step</button>
    </section>
  );
};
