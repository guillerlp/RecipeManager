import { useId, useRef, useState } from 'react';
import styles from './RecipeDetailTabs.module.css';

interface RecipeDetailTabsProps {
  ingredients: React.ReactNode;
  method: React.ReactNode;
}

const LABELS = ['Ingredients', 'Method'] as const;

// The WAI-ARIA Authoring Practices "tabs" pattern, with automatic activation. Hand-written because
// HTML has no native tabs element — which is exactly why RecipeDetailTabs.test.tsx drives it by keyboard.
export const RecipeDetailTabs = ({ ingredients, method }: RecipeDetailTabsProps) => {
  const id = useId();
  const [selected, setSelected] = useState(0);
  const tabs = useRef<(HTMLButtonElement | null)[]>([]);
  const panels = [ingredients, method];
  const last = LABELS.length - 1;

  const select = (index: number) => {
    setSelected(index);
    tabs.current[index]?.focus();
  };

  const onKeyDown = (event: React.KeyboardEvent) => {
    switch (event.key) {
      case 'ArrowRight': select(selected === last ? 0 : selected + 1); break;
      case 'ArrowLeft': select(selected === 0 ? last : selected - 1); break;
      case 'Home': select(0); break;
      case 'End': select(last); break;
      default: return;
    }
    event.preventDefault(); // Home/End would otherwise also scroll the page
  };

  return (
    <div className={styles.tabs}>
      <div role="tablist" aria-label="Recipe sections" className={styles.list} onKeyDown={onKeyDown}>
        {LABELS.map((label, index) => (
          <button
            key={label}
            ref={element => { tabs.current[index] = element; }}
            type="button"
            role="tab"
            id={`${id}-tab-${index}`}
            aria-selected={index === selected}
            aria-controls={`${id}-panel-${index}`}
            tabIndex={index === selected ? 0 : -1}
            className={`${styles.tab} ${index === selected ? styles.selected : ''}`}
            onClick={() => setSelected(index)}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Both panels stay mounted, only `hidden`, so nothing inside the inactive one is torn down. */}
      {panels.map((panel, index) => (
        <div
          key={LABELS[index]}
          role="tabpanel"
          id={`${id}-panel-${index}`}
          aria-labelledby={`${id}-tab-${index}`}
          hidden={index !== selected}
          tabIndex={0}
          className={styles.panel}
        >
          {panel}
        </div>
      ))}
    </div>
  );
};
