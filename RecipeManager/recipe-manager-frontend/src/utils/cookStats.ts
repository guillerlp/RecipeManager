// src/utils/cookStats.ts
import type { Recipe } from '@/types';

export interface CookSummary {
  totalCooks: number;
  mostCooked: Recipe | null;
  recentlyCooked: Recipe[];
}

const lastCooked = (recipe: Recipe) => (recipe.lastCookedAt ? Date.parse(recipe.lastCookedAt) : 0);

// ponytail: aggregated in the browser over the full, unpaginated lists (SEC-07). When R-11 paginates GET /api/recipes,
// this becomes a server stats query — a page of recipes cannot produce a catalogue total.
export const summariseCooks = (recipes: readonly Recipe[]): CookSummary => {
  const byRecent = recipes.filter(r => r.cookCount > 0).sort((a, b) => lastCooked(b) - lastCooked(a));
  // Scanned newest first and replaced only on a strictly higher count, so a tie goes to the more recent cook.
  const mostCooked = byRecent.reduce<Recipe | null>(
    (best, r) => (best === null || r.cookCount > best.cookCount ? r : best), null);

  return {
    totalCooks: byRecent.reduce((sum, r) => sum + r.cookCount, 0),
    mostCooked,
    recentlyCooked: byRecent.slice(0, 3),
  };
};
