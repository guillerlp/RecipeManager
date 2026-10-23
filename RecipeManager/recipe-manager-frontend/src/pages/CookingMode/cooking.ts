// Cooking mode's derivations (spec 017 §8.4–§8.6), pure so they can be table-tested without rendering.
import { MAX_SERVINGS, MIN_SERVINGS } from '@/components/ui/Recipe/ServingsStepper';
import type { Ingredient, Recipe } from '@/types';

// Only a plain positive integer counts: "2.5" or "1e2" from a hand-edited URL falls back rather than being rounded.
const asInteger = (raw: string | null): number | null => (raw !== null && /^\d+$/.test(raw) ? Number(raw) : null);

// The URL is user-editable, so it is clamped rather than trusted: 99 on a 3-step recipe is the last step.
export const parseStep = (raw: string | null, stepCount: number): number => {
  const step = asInteger(raw);
  return step === null || step < 1 ? 1 : Math.min(step, stepCount);
};

// Out of range means "not what Detail would ever send", so it falls back to the recipe's own servings.
// No written servings: nothing to scale from, so amounts stay as written whatever the URL says.
export const parseServings = (raw: string | null, written: number | null): number | null => {
  if (written === null) return null;
  const servings = asInteger(raw);
  return servings === null || servings < MIN_SERVINGS || servings > MAX_SERVINGS ? written : servings;
};

export type IngredientLists =
  | { mode: 'all'; all: Ingredient[] }
  | { mode: 'byStep'; forStep: Ingredient[]; used: Ingredient[]; alsoNeeded: Ingredient[] };

// Decided per recipe, not per step (spec 017 §8.5): references are optional, and a recipe that never uses
// them would otherwise show an empty "For this step" everywhere. Every list keeps the recipe's ingredient order.
export const ingredientLists = (
  recipe: Pick<Recipe, 'ingredients' | 'instructions'>,
  stepIndex: number,
): IngredientLists => {
  const { ingredients, instructions } = recipe;
  if (!instructions.some(step => step.ingredientIds.length > 0)) return { mode: 'all', all: ingredients };

  const current = new Set(instructions[stepIndex]?.ingredientIds ?? []);
  const earlier = new Set(instructions.slice(0, stepIndex).flatMap(step => step.ingredientIds));
  const anywhere = new Set(instructions.flatMap(step => step.ingredientIds));
  return {
    mode: 'byStep',
    forStep: ingredients.filter(i => current.has(i.id)),
    used: ingredients.filter(i => earlier.has(i.id) && !current.has(i.id)),
    alsoNeeded: ingredients.filter(i => !anywhere.has(i.id)),
  };
};
