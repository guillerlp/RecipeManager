// Presentation only: the one place an ingredient's stored quantity becomes what the cook reads. Shared by the
// detail rail and cooking mode (spec 017 §8.6), so the two screens cannot show different amounts.
import type { Ingredient, UnitSystem } from '@/types';
import { type AmountText, formatAmount, scaleQuantity } from './quantity';
import { convertQuantity } from './units';

// scale → convert → format (spec 012 §8.5): conversion sees the scaled amount, so its thresholds
// (1000 g, 1 lb, ¼ cup) apply to what the cook will actually measure.
export const shownAmount = (
  ingredient: Ingredient,
  writtenServings: number | null,
  servings: number | null,
  unitSystem: UnitSystem,
): { amount: AmountText | null; converted: boolean } => {
  // Null written servings: a draft with no base to scale from, so amounts show as written.
  const scaled = writtenServings === null || servings === null
    ? ingredient.quantity ?? null
    : scaleQuantity(ingredient.quantity, writtenServings, servings);
  const shown = convertQuantity(scaled, ingredient.unit, unitSystem);
  return { amount: formatAmount(shown.quantity, shown.unit), converted: shown.converted };
};
