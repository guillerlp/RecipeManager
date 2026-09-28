// src/components/ui/SegmentedControl/SegmentedControl.tsx

import styles from './SegmentedControl.module.css';

interface SegmentedControlProps<T extends string> {
  legend: string;
  name: string;
  options: readonly { value: T; label: string }[];
  value: T;
  onChange: (value: T) => void;
}

// Native radios, not buttons with aria-pressed: a native radio group gives arrow-key navigation,
// a single tab stop, and the correct screen-reader announcement for free. role="switch" does not
// apply — a switch is binary and these have three states. Generic over the option type so each
// caller keeps its own union ('light' | 'dark' | 'system', UnitSystem) end to end.
export const SegmentedControl = <T extends string,>({ legend, name, options, value, onChange }: SegmentedControlProps<T>) => (
  <fieldset className={styles.group} role="radiogroup">
    {/* Visually hidden, not aria-label: a <legend> is the native way to name a fieldset,
        and role="radiogroup" doesn't stop the browser using it for the accessible name. */}
    <legend className={styles.legend}>{legend}</legend>
    {options.map(option => {
      const selected = value === option.value;
      return (
        <label key={option.value} className={selected ? `${styles.segment} ${styles.selected}` : styles.segment}>
          <input
            type="radio"
            name={name}
            className={styles.input}
            checked={selected}
            onChange={() => onChange(option.value)}
          />
          {option.label}
        </label>
      );
    })}
  </fieldset>
);
