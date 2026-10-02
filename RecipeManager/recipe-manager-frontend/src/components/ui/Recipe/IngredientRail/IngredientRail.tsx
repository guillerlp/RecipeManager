import type { Ingredient, UnitSystem } from '@/types';
import { formatAmount, scaleQuantity } from '@/utils/quantity';
import { convertQuantity } from '@/utils/units';
import { ServingsStepper } from '../ServingsStepper';
import styles from './IngredientRail.module.css';

interface IngredientRailProps {
  ingredients: Ingredient[];
  // Null for a draft that has no servings yet: there is no base to scale from, so amounts show as written.
  writtenServings: number | null;
  servings: number | null;
  onServingsChange: (next: number) => void;
  unitSystem: UnitSystem;
}

// Derived during render, never stored: the amounts are a pure function of the stored quantities,
// the current servings and the unit system, so they cannot drift out of sync (RecipeList.filteredRecipes
// is the same idea). No useMemo — a few dozen multiplications per click is not a cost worth caching.
export const IngredientRail = ({ ingredients, writtenServings, servings, onServingsChange, unitSystem }: IngredientRailProps) => {
  // scale → convert → format (spec 012 §8.5): conversion sees the scaled amount, so its thresholds
  // (1000 g, 1 lb, ¼ cup) apply to what the cook will actually measure.
  const rows = ingredients.map(ingredient => {
    const scaled = writtenServings === null || servings === null
      ? ingredient.quantity ?? null
      : scaleQuantity(ingredient.quantity, writtenServings, servings);
    const shown = convertQuantity(scaled, ingredient.unit, unitSystem);
    return { ingredient, amount: formatAmount(shown.quantity, shown.unit), converted: shown.converted };
  });
  const anyConverted = rows.some(row => row.converted);

  return (
    <aside className={styles.rail} aria-labelledby="ingredients-heading">
      <div className={styles.head}>
        <h2 id="ingredients-heading" className={styles.heading}>Ingredients</h2>
        {servings !== null && <ServingsStepper value={servings} onChange={onServingsChange} />}
      </div>

      {/* role="list": see MethodSteps — list-style: none drops list semantics in VoiceOver. */}
      <ul role="list" className={styles.list}>
        {rows.map(({ ingredient, amount }) => {
          return (
            <li key={ingredient.id} className={styles.row}>
              {amount && (
                <span className={styles.amount}>
                  {amount.visible === amount.spoken ? (
                    // A bare count ("2.5") reads fine as painted; a second, hidden copy would be
                    // announced twice.
                    amount.visible
                  ) : (
                    <>
                      <span aria-hidden="true">{amount.visible}</span>
                      <span className={styles.spoken}>{amount.spoken}</span>
                    </>
                  )}
                </span>
              )}
              <span className={styles.name}>
                {ingredient.name}
                {ingredient.notes && `, ${ingredient.notes}`}
              </span>
            </li>
          );
        })}
      </ul>

      {(servings !== null || anyConverted) && (
        <p className={styles.note}>
          {servings !== null && `Scaled for ${servings}. Change the number and every quantity follows.`}
          {anyConverted && " Converted from the recipe's own units."}
        </p>
      )}
    </aside>
  );
};
