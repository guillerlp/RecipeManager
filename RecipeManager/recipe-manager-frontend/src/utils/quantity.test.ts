import { describe, expect, it } from 'vitest';
import { formatAmount, formatQuantity, scaleQuantity, toKitchenFraction } from './quantity';

describe('scaleQuantity', () => {
  it('leaves a null quantity null', () => {
    expect(scaleQuantity(null, 4, 8)).toBeNull();
    expect(scaleQuantity(undefined, 4, 8)).toBeNull();
  });

  it('scales by current / written', () => {
    expect(scaleQuantity(1.6, 4, 5)).toBeCloseTo(2);
    expect(scaleQuantity(2, 4, 8)).toBe(4);
  });

  it('comes back to the stored amount after stepping away and back (never scales a scaled value)', () => {
    const stored = 1.6;
    scaleQuantity(stored, 4, 7);
    expect(formatQuantity(scaleQuantity(stored, 4, 4)!, 'Kilogram')).toBe('1.6');
  });
});

describe('toKitchenFraction', () => {
  it.each<[number, string]>([
    [0.75, '¾'],
    [1.25, '1¼'],
    [0.5, '½'],
    [0.34, '⅓'],
    [2 / 3, '⅔'],
    [0.375, '⅜'],
    [3, '3'],
    [1.97, '2'],
    [0.01, '⅛'],
    [0, '0'],
  ])('%f → %s', (value, expected) => {
    expect(toKitchenFraction(value)).toBe(expected);
  });
});

describe('formatQuantity', () => {
  it.each<[number, Parameters<typeof formatQuantity>[1], string]>([
    [1.25, 'Teaspoon', '1¼'],
    [0.75, 'Tablespoon', '¾'],
    [0.34, 'Cup', '⅓'],
    [1.5, 'FluidOunce', '1½'],
    [2, 'Kilogram', '2'],
    [450, 'Gram', '450'],
    [452.6, 'Gram', '453'],
    [1.5, null, '1.5'],
    [1.25, 'Piece', '1.3'],
    [0.25, 'Litre', '0.25'],
    [0.333333, 'Litre', '0.33'],
    [0.001, 'Gram', '0.001'],
  ])('%f %s → %s', (value, unit, expected) => {
    expect(formatQuantity(value, unit)).toBe(expected);
  });
});

describe('formatAmount', () => {
  it('returns null when there is no quantity', () => {
    expect(formatAmount(null, 'Gram')).toBeNull();
  });

  it('never pluralises an abbreviation, and spells it out for screen readers', () => {
    expect(formatAmount(3, 'Tablespoon')).toEqual({ visible: '3 tbsp', spoken: '3 tablespoons' });
    expect(formatAmount(1, 'Tablespoon')).toEqual({ visible: '1 tbsp', spoken: '1 tablespoon' });
    expect(formatAmount(2, 'FluidOunce')).toEqual({ visible: '2 fl oz', spoken: '2 fluid ounces' });
  });

  it('pluralises words above one and keeps them singular at or below one', () => {
    expect(formatAmount(2, 'Clove')).toEqual({ visible: '2 cloves', spoken: '2 cloves' });
    expect(formatAmount(1, 'Pinch')).toEqual({ visible: '1 pinch', spoken: '1 pinch' });
    expect(formatAmount(0.5, 'Cup')).toEqual({ visible: '½ cup', spoken: '½ cup' });
    expect(formatAmount(1.5, 'Cup')).toEqual({ visible: '1½ cups', spoken: '1½ cups' });
  });

  it('chooses singular or plural from the number it shows, not the unrounded value (BUG-17)', () => {
    expect(formatAmount(1.05, 'Cup')).toEqual({ visible: '1 cup', spoken: '1 cup' });
    expect(formatAmount(1.02, 'Kilogram')).toEqual({ visible: '1 kg', spoken: '1 kilogram' });
    expect(formatAmount(0.97, 'Cup')).toEqual({ visible: '1 cup', spoken: '1 cup' });
    expect(formatAmount(1.2, 'Cup')).toEqual({ visible: '1¼ cups', spoken: '1¼ cups' });
  });

  it('renders a bare number when there is no unit', () => {
    expect(formatAmount(2, null)).toEqual({ visible: '2', spoken: '2' });
  });
});
