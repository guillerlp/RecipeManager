import { describe, expect, it } from 'vitest';
import type { Recipe } from '@/types';
import { summariseCooks } from '@/utils/cookStats';

const recipe = (id: string, cookCount: number, lastCookedAt: string | null = null, status: Recipe['status'] = 'Published'): Recipe =>
  ({ id, title: id, status, ingredients: [], instructions: [], tags: [], cookCount, lastCookedAt });

describe('summariseCooks', () => {
  it('is empty for no recipes, or none cooked', () => {
    expect(summariseCooks([])).toEqual({ totalCooks: 0, mostCooked: null, recentlyCooked: [] });
    expect(summariseCooks([recipe('a', 0)])).toEqual({ totalCooks: 0, mostCooked: null, recentlyCooked: [] });
  });

  it('totals every cook, drafts included', () => {
    const { totalCooks } = summariseCooks([
      recipe('a', 3, '2026-10-01T10:00:00Z'),
      recipe('b', 2, '2026-10-02T10:00:00Z', 'Draft'),
    ]);

    expect(totalCooks).toBe(5);
  });

  it('picks the most cooked, breaking a tie by the more recent cook', () => {
    const older = recipe('older', 4, '2026-09-01T10:00:00Z');
    const newer = recipe('newer', 4, '2026-10-01T10:00:00Z');

    expect(summariseCooks([older, newer]).mostCooked?.id).toBe('newer');
    expect(summariseCooks([newer, older]).mostCooked?.id).toBe('newer');
  });

  it('lists the three most recently cooked, newest first, skipping the uncooked', () => {
    const { recentlyCooked } = summariseCooks([
      recipe('mon', 1, '2026-10-05T10:00:00Z'),
      recipe('never', 0),
      recipe('thu', 1, '2026-10-08T10:00:00Z'),
      recipe('sep', 9, '2026-09-01T10:00:00Z'),
      recipe('sun', 1, '2026-10-11T10:00:00Z'),
    ]);

    expect(recentlyCooked.map(r => r.id)).toEqual(['sun', 'thu', 'mon']);
  });
});
