import styles from './ServingsStepper.module.css';

// The Servings validator's bounds (RecipeValidationRules): the stepper never shows a count the
// API would refuse to store.
export const MIN_SERVINGS = 1;
export const MAX_SERVINGS = 999;

interface ServingsStepperProps {
  value: number;
  onChange: (next: number) => void;
}

export const ServingsStepper = ({ value, onChange }: ServingsStepperProps) => (
  <div role="group" aria-label="Servings" className={styles.stepper}>
    <button
      type="button"
      className={styles.button}
      aria-label="Decrease servings"
      disabled={value <= MIN_SERVINGS}
      onClick={() => onChange(value - 1)}
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
      disabled={value >= MAX_SERVINGS}
      onClick={() => onChange(value + 1)}
    >
      +
    </button>
  </div>
);
