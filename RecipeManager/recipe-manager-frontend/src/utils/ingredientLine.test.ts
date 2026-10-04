import { describe, expect, it } from 'vitest';
import type { Unit } from '@/types';
import { describeIngredient, parseIngredientLine, serialiseIngredient, type ParsedIngredient } from '@/utils/ingredientLine';
import { UNIT_NAMES } from '@/utils/quantity';

const value = (quantity: number | null, unit: Unit | null, name: string, notes: string | null = null): ParsedIngredient =>
  ({ quantity, unit, name, notes });

describe('parseIngredientLine', () => {
  // Spec 010 §9 examples, then spec 015 §8.2.
  it.each([
    ['2 tbsp butter, cold', value(2, 'Tablespoon', 'butter', 'cold')],
    ['salt to taste', value(null, null, 'salt to taste')],
    ['3 eggs', value(3, null, 'eggs')],
    ['1 1/2 cups flour', value(1.5, 'Cup', 'flour')],
    ['1/2 tsp salt', value(0.5, 'Teaspoon', 'salt')],
    ['1.5 kg potatoes', value(1.5, 'Kilogram', 'potatoes')],
    ['200g flour', value(200, 'Gram', 'flour')],
    ['1.5kg potatoes', value(1.5, 'Kilogram', 'potatoes')],
    ['2 fl oz milk', value(2, 'FluidOunce', 'milk')],
    ['2 TABLESPOONS Butter', value(2, 'Tablespoon', 'Butter')],
    ['tbsp butter', value(null, null, 'tbsp butter')],
    ['3-4 eggs', value(null, null, '3-4 eggs')],
    ['2x eggs', value(null, null, '2x eggs')],
    ['1/3 cup stock', value(0.333, 'Cup', 'stock')],
    ['  2   cloves   garlic ,  crushed  ', value(2, 'Clove', 'garlic', 'crushed')],
    ['butter, cold, cubed', value(null, null, 'butter', 'cold, cubed')],
    ['butter,', value(null, null, 'butter')],
  ])('parses %j', (line, expected) => {
    expect(parseIngredientLine(line)).toEqual({ ok: true, value: expected });
  });

  it.each([
    ['', "Add the ingredient's name"],
    ['2', "Add the ingredient's name"],
    ['2 cups', "Add the ingredient's name"],
    [', cold', "Add the ingredient's name"],
    ['0 eggs', 'Quantity must be more than zero'],
    ['1/0 cup milk', "That fraction doesn't work"],
    ['200000 g flour', 'Quantity is too large'],
    [`2 ${'a'.repeat(201)}`, 'Keep the name to 200 characters'],
    [`butter, ${'a'.repeat(201)}`, 'Keep the note to 200 characters'],
  ])('rejects %j', (line, error) => {
    expect(parseIngredientLine(line)).toEqual({ ok: false, error });
  });
});

describe('serialiseIngredient', () => {
  it('writes the stored quantity, not the rounded display form', () => {
    expect(serialiseIngredient(value(0.333, 'Cup', 'stock'))).toBe('0.333 cups stock');
    expect(serialiseIngredient(value(12.34, 'Gram', 'yeast'))).toBe('12.34 g yeast');
  });

  it('uses the singular word for exactly one of an unabbreviated unit', () => {
    expect(serialiseIngredient(value(1, 'Cup', 'rice'))).toBe('1 cup rice');
    expect(serialiseIngredient(value(2, 'Clove', 'garlic', 'crushed'))).toBe('2 cloves garlic, crushed');
    expect(serialiseIngredient(value(null, null, 'salt to taste'))).toBe('salt to taste');
  });

  // The edit screen shows serialised lines; a line the parser reads back differently would change the recipe.
  it.each(Object.keys(UNIT_NAMES) as Unit[])('round-trips %s', unit => {
    for (const quantity of [0.333, 1, 2.5, 100000]) {
      const original = value(quantity, unit, 'thing', 'note');
      expect(parseIngredientLine(serialiseIngredient(original))).toEqual({ ok: true, value: original });
    }
  });
});

describe('describeIngredient', () => {
  it('shows the amount, a dot, then the name and notes', () => {
    expect(describeIngredient(value(2, 'Tablespoon', 'butter', 'cold'))).toBe('2 tbsp · butter, cold');
    expect(describeIngredient(value(3, null, 'eggs'))).toBe('3 · eggs');
    expect(describeIngredient(value(null, null, 'salt to taste'))).toBe('salt to taste');
  });
});
