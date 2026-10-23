import { describe, expect, it } from 'vitest';
import type { Ingredient, InstructionStep } from '@/types';
import { ingredientLists, parseServings, parseStep } from './cooking';

describe('parseStep', () => {
  it.each([
    ['2', 2],
    ['99', 3],
    ['0', 1],
    ['-1', 1],
    ['abc', 1],
    ['2.5', 1],
    [null, 1],
  ])('reads %s on a 3-step recipe as step %i', (raw, expected) => {
    expect(parseStep(raw, 3)).toBe(expected);
  });
});

describe('parseServings', () => {
  it.each([
    ['6', 4, 6],
    ['999', 4, 999],
    ['0', 4, 4],
    ['abc', 4, 4],
    ['5000', 4, 4],
    ['1.5', 4, 4],
    [null, 4, 4],
    ['6', null, null],
  ])('reads %s with %s written servings as %s', (raw, written, expected) => {
    expect(parseServings(raw, written)).toBe(expected);
  });
});

const ingredient = (id: string): Ingredient =>
  ({ id, quantity: null, unit: null, name: id.toUpperCase(), notes: null });
const step = (ingredientIds: string[]): InstructionStep =>
  ({ id: `s-${ingredientIds.join('')}`, text: 'Do it.', durationMinutes: null, ingredientIds });

const [A, B, C, D] = ['a', 'b', 'c', 'd'].map(ingredient);
const ingredients = [A, B, C, D];

describe('ingredientLists', () => {
  it('lists every ingredient when no step references any', () => {
    const recipe = { ingredients, instructions: [step([]), step([])] };
    expect(ingredientLists(recipe, 1)).toEqual({ mode: 'all', all: ingredients });
  });

  it('splits into this step, already used, and also needed', () => {
    const recipe = { ingredients, instructions: [step(['a']), step(['b']), step(['c'])] };
    expect(ingredientLists(recipe, 2)).toEqual({ mode: 'byStep', forStep: [C], used: [A, B], alsoNeeded: [D] });
  });

  it('moves an ingredient back to this step when a later step uses it again', () => {
    const recipe = { ingredients, instructions: [step(['a']), step(['b']), step(['a'])] };
    expect(ingredientLists(recipe, 1)).toMatchObject({ forStep: [B], used: [A] });
    expect(ingredientLists(recipe, 2)).toMatchObject({ forStep: [A], used: [B] });
  });

  it('keeps ingredient order, not reference order', () => {
    const recipe = { ingredients, instructions: [step(['b', 'a'])] };
    expect(ingredientLists(recipe, 0)).toMatchObject({ forStep: [A, B] });
  });

  it('shows a repeated reference once and ignores an unknown id', () => {
    const recipe = { ingredients, instructions: [step(['a', 'a', 'ghost'])] };
    expect(ingredientLists(recipe, 0)).toMatchObject({ forStep: [A] });
  });

  it('gives a step without references an empty list in a by-step recipe', () => {
    const recipe = { ingredients, instructions: [step(['a']), step([])] };
    expect(ingredientLists(recipe, 1)).toEqual({ mode: 'byStep', forStep: [], used: [A], alsoNeeded: [B, C, D] });
  });
});
