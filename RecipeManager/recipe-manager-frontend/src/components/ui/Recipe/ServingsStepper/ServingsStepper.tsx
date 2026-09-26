import styles from './ServingsStepper.module.css';

// The Servings validator's bounds (RecipeValidationRules): the stepper never shows a count the
// API would refuse to store.
export const MIN_SERVINGS = 1;
export const MAX_SERVINGS = 999;

interface ServingsStepperProps {
  value: number;
  onChange: (next: number) => void;
}

// aria-disabled rather than the disabled attribute: pressing − down to 1 would otherwise disable the
// very button that has focus, and the browser would drop focus to <body>, losing a keyboard or
// screen-reader user's place. The button stays focusable, is announced as unavailable, and does nothing.
export const ServingsStepper = ({ value, onChange }: ServingsStepperProps) => {
  const atMin = value <= MIN_SERVINGS;
  const atMax = value >= MAX_SERVINGS;

  return (
    <div role="group" aria-label="Servings" className={styles.stepper}>
      <button
        type="button"
        className={styles.button}
        aria-label="Decrease servings"
        aria-disabled={atMin}
        onClick={() => { if (!atMin) onChange(value - 1); }}
      >
        −
      </button>
      {/* <output> is role="status": the new count is announced politely after each press. */}
      <output className={styles.value} aria-live="polite">
        {value}
      </output>
      <button
        type="button"
        className={styles.button}
        aria-label="Increase servings"
        aria-disabled={atMax}
        onClick={() => { if (!atMax) onChange(value + 1); }}
      >
        +
      </button>
    </div>
  );
};
