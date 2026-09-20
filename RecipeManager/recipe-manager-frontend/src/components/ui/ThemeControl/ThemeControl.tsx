// src/components/ui/ThemeControl/ThemeControl.tsx

import { useTheme } from '@/hooks';
import { ThemePreference } from '@/types/theme';
import styles from './ThemeControl.module.css';

const OPTIONS: readonly { value: ThemePreference; label: string }[] = [
  { value: 'light', label: 'Light' },
  { value: 'dark', label: 'Dark' },
  { value: 'system', label: 'System' },
];

// Three native radios, not three buttons with aria-pressed: a native radio group gives
// arrow-key navigation, a single tab stop, and the correct screen-reader announcement for
// free. role="switch" does not apply here — a switch is binary and this has three states.
export const ThemeControl: React.FC = () => {
  const { preference, setPreference } = useTheme();

  return (
    <fieldset className={styles.group} role="radiogroup">
      {/* Visually hidden, not aria-label: a <legend> is the native way to name a fieldset,
          and role="radiogroup" doesn't stop the browser using it for the accessible name. */}
      <legend className={styles.legend}>Theme</legend>
      {OPTIONS.map(({ value, label }) => {
        const selected = preference === value;
        return (
          <label
            key={value}
            className={selected ? `${styles.segment} ${styles.selected}` : styles.segment}
          >
            <input
              type="radio"
              name="theme"
              className={styles.input}
              checked={selected}
              onChange={() => setPreference(value)}
            />
            {label}
          </label>
        );
      })}
    </fieldset>
  );
};

export default ThemeControl;
