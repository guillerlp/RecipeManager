import { useId } from 'react';
import { IngredientAmount } from '@/components/ui/Recipe';
import type { Ingredient, UnitSystem } from '@/types';
import { shownAmount } from '@/utils/ingredientAmount';
import type { IngredientLists as Lists } from './cooking';
import styles from './CookingModePage.module.css';

interface Scaling {
  writtenServings: number | null;
  servings: number | null;
  unitSystem: UnitSystem;
}

interface ListProps extends Scaling {
  title: string;
  items: Ingredient[];
  used?: boolean;
}

// The heading names the list (aria-labelledby), so a screen reader announces "Already used, list, 2 items".
const List = ({ title, items, used = false, writtenServings, servings, unitSystem }: ListProps) => {
  const id = useId();
  if (items.length === 0) return null;
  return (
    <section className={styles.listSection}>
      <h2 id={id} className={styles.listTitle}>{title}</h2>
      {/* role="list": list-style: none drops list semantics in VoiceOver (see MethodSteps). */}
      <ul role="list" aria-labelledby={id} className={styles.list}>
        {items.map(ingredient => {
          const { amount } = shownAmount(ingredient, writtenServings, servings, unitSystem);
          return (
            <li key={ingredient.id} className={used ? `${styles.item} ${styles.used}` : styles.item}>
              {amount && <IngredientAmount amount={amount} className={styles.amount} />}
              <span>
                {ingredient.name}
                {ingredient.notes && `, ${ingredient.notes}`}
              </span>
              {/* The strike-through is styling; this says the same thing to a screen reader. */}
              {used && <span className={styles.visuallyHidden}>, used</span>}
            </li>
          );
        })}
      </ul>
    </section>
  );
};

export const IngredientLists = ({ lists, ...scaling }: Scaling & { lists: Lists }) =>
  lists.mode === 'all' ? (
    <List title="Ingredients" items={lists.all} {...scaling} />
  ) : (
    <>
      <List title="For this step" items={lists.forStep} {...scaling} />
      <List title="Already used" items={lists.used} used {...scaling} />
      <List title="Also needed" items={lists.alsoNeeded} {...scaling} />
    </>
  );
