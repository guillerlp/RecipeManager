import type { Ingredient } from '@/types';
import { formatAmount, scaleQuantity } from '@/utils/quantity';
import { ServingsStepper } from '../ServingsStepper';
import styles from './IngredientRail.module.css';

interface IngredientRailProps {
  ingredients: Ingredient[];
  writtenServings: number;
  servings: number;
  onServingsChange: (next: number) => void;
}

// Derived during render, never stored: the amounts are a pure function of the stored quantities
// and the current servings, so they cannot drift out of sync (RecipeList.filteredRecipes is the
// same idea). No useMemo — a few dozen multiplications per click is not a cost worth caching.
export const IngredientRail = ({ ingredients, writtenServings, servings, onServingsChange }: IngredientRailProps) => (
  <aside className={styles.rail} aria-labelledby="ingredients-heading">
    <div className={styles.head}>
      <h2 id="ingredients-heading" className={styles.heading}>Ingredients</h2>
      <ServingsStepper value={servings} onChange={onServingsChange} />
    </div>

    {/* role="list": see MethodSteps — list-style: none drops list semantics in VoiceOver. */}
    <ul role="list" className={styles.list}>
      {ingredients.map(ingredient => {
        const amount = formatAmount(scaleQuantity(ingredient.quantity, writtenServings, servings), ingredient.unit);
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

    <p className={styles.note}>Scaled for {servings}. Change the number and every quantity follows.</p>
  </aside>
);
