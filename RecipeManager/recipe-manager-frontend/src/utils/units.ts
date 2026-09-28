// Presentation only (ADR-022, spec 012 §8.5): converts for display, never written back.
import type { Unit, UnitSystem } from '@/types';

export interface ConvertedQuantity {
  quantity: number | null;
  unit: Unit | null;
  converted: boolean;
}

// US customary (spec 012 §14): a UK or Australian cup is a different size.
const G_PER_OZ = 28.3495;
const G_PER_LB = 453.592;
const ML_PER_FL_OZ = 29.5735;
const ML_PER_CUP = 236.588;

const GRAMS: Partial<Record<Unit, number>> = { Gram: 1, Kilogram: 1000, Ounce: G_PER_OZ, Pound: G_PER_LB };
const MILLILITRES: Partial<Record<Unit, number>> = { Millilitre: 1, Litre: 1000, FluidOunce: ML_PER_FL_OZ, Cup: ML_PER_CUP };
const NATIVE: Record<Exclude<UnitSystem, 'asWritten'>, ReadonlySet<Unit>> = {
  metric: new Set<Unit>(['Gram', 'Kilogram', 'Millilitre', 'Litre']),
  imperial: new Set<Unit>(['Ounce', 'Pound', 'FluidOunce', 'Cup']),
};

const to = (quantity: number, unit: Unit): ConvertedQuantity => ({ quantity, unit, converted: true });

// Runs after scaling, so the thresholds see the amount the cook will actually measure.
export const convertQuantity = (
  quantity: number | null,
  unit: Unit | null | undefined,
  system: UnitSystem,
): ConvertedQuantity => {
  const asWritten = { quantity, unit: unit ?? null, converted: false };
  if (quantity === null || !unit || system === 'asWritten' || NATIVE[system].has(unit)) return asWritten;

  const perGram = GRAMS[unit];
  if (perGram !== undefined) {
    const g = quantity * perGram;
    if (system === 'metric') return g >= 1000 ? to(g / 1000, 'Kilogram') : to(g, 'Gram');
    return g >= G_PER_LB ? to(g / G_PER_LB, 'Pound') : to(g / G_PER_OZ, 'Ounce');
  }

  const perMl = MILLILITRES[unit];
  if (perMl !== undefined) {
    const ml = quantity * perMl;
    if (system === 'metric') return ml >= 1000 ? to(ml / 1000, 'Litre') : to(ml, 'Millilitre');
    return ml >= ML_PER_CUP / 4 ? to(ml / ML_PER_CUP, 'Cup') : to(ml / ML_PER_FL_OZ, 'FluidOunce');
  }

  return asWritten; // spoons and counts: the same in both systems
};
