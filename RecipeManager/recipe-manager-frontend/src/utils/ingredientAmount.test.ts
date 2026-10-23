import { describe, expect, it } from 'vitest';
import type { Ingredient } from '@/types';
import { shownAmount } from './ingredientAmount';
import { formatAmount } from './quantity';
import { convertQuantity } from './units';

const ingredient = (overrides: Partial<Ingredient>): Ingredient =>
  ({ id: 'i1', quantity: null, unit: null, name: 'thing', notes: null, ...overrides });

describe('shownAmount', () => {
  it('scales by the servings ratio', () => {
    const { amount, converted } = shownAmount(ingredient({ quantity: 2 }), 4, 6, 'asWritten');
    expect(amount?.visible).toBe('3');
    expect(converted).toBe(false);
  });

  it('leaves amounts as written when the recipe has no written servings', () => {
    expect(shownAmount(ingredient({ quantity: 2 }), null, 6, 'asWritten').amount?.visible).toBe('2');
  });

  it('returns no amount for an ingredient without a quantity', () => {
    expect(shownAmount(ingredient({ name: 'black pepper' }), 4, 6, 'asWritten').amount).toBeNull();
  });

  it('converts the scaled amount, not the written one', () => {
    const shown = convertQuantity(1200, 'Gram', 'imperial');
    const result = shownAmount(ingredient({ quantity: 600, unit: 'Gram' }), 4, 8, 'imperial');
    expect(result.amount).toEqual(formatAmount(shown.quantity, shown.unit));
    expect(result.converted).toBe(true);
  });
});
