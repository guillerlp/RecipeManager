import { describe, expect, it } from 'vitest';
import type { Unit, UnitSystem } from '@/types';
import { formatAmount } from './quantity';
import { convertQuantity } from './units';

const shown = (quantity: number, unit: Unit, system: UnitSystem) => {
  const c = convertQuantity(quantity, unit, system);
  return formatAmount(c.quantity, c.unit)?.visible;
};

describe('convertQuantity', () => {
  it('converts nothing when the preference is As written', () => {
    expect(convertQuantity(8, 'Ounce', 'asWritten')).toEqual({ quantity: 8, unit: 'Ounce', converted: false });
  });

  it('leaves a unit already in the chosen system as written', () => {
    expect(convertQuantity(500, 'Gram', 'metric')).toEqual({ quantity: 500, unit: 'Gram', converted: false });
    expect(convertQuantity(2, 'Cup', 'imperial')).toEqual({ quantity: 2, unit: 'Cup', converted: false });
  });

  it('never converts spoons, counts, a missing unit, or a missing quantity', () => {
    for (const system of ['metric', 'imperial'] as const) {
      expect(convertQuantity(1, 'Tablespoon', system).converted).toBe(false);
      expect(convertQuantity(2, 'Teaspoon', system).converted).toBe(false);
      expect(convertQuantity(3, 'Clove', system).converted).toBe(false);
      expect(convertQuantity(2, null, system)).toEqual({ quantity: 2, unit: null, converted: false });
      expect(convertQuantity(null, 'Gram', system)).toEqual({ quantity: null, unit: 'Gram', converted: false });
    }
    expect(shown(1, 'Tablespoon', 'metric')).toBe('1 tbsp');
  });

  it('converts imperial weights to grams, and to kilograms from 1000 g', () => {
    expect(shown(8, 'Ounce', 'metric')).toBe('227 g');
    expect(convertQuantity(35, 'Ounce', 'metric').unit).toBe('Gram'); // 992 g
    const kg = convertQuantity(36, 'Ounce', 'metric'); // 1020.6 g
    expect(kg.unit).toBe('Kilogram');
    expect(kg.quantity).toBeCloseTo(1.0206, 4);
    expect(shown(3, 'Pound', 'metric')).toBe('1.4 kg');
  });

  it('converts imperial volumes to millilitres, and to litres from 1000 ml', () => {
    expect(shown(4, 'FluidOunce', 'metric')).toBe('118 ml');
    expect(shown(2, 'Cup', 'metric')).toBe('473 ml');
    expect(shown(5, 'Cup', 'metric')).toBe('1.2 l');
  });

  it('converts metric weights to ounces, and to pounds from 1 lb', () => {
    expect(shown(200, 'Gram', 'imperial')).toBe('7.1 oz');
    expect(convertQuantity(450, 'Gram', 'imperial').unit).toBe('Ounce');
    expect(convertQuantity(453.592, 'Gram', 'imperial')).toEqual({ quantity: 1, unit: 'Pound', converted: true });
    expect(shown(1, 'Kilogram', 'imperial')).toBe('2.2 lb');
  });

  it('converts metric volumes to fluid ounces, and to cups from a quarter cup', () => {
    expect(shown(500, 'Millilitre', 'imperial')).toBe('2⅛ cups');
    expect(shown(250, 'Millilitre', 'imperial')).toBe('1 cup');
    expect(convertQuantity(60, 'Millilitre', 'imperial').unit).toBe('Cup');
    expect(convertQuantity(55, 'Millilitre', 'imperial').unit).toBe('FluidOunce');
    expect(shown(1, 'Litre', 'imperial')).toBe('4¼ cups');
  });
});
