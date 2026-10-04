import { describe, expect, it } from 'vitest';
import type { Recipe } from '@/types';
import {
  emptyForm, emptyIngredient, emptyStep, formReducer, fromRecipe, fromServerErrors, hasErrors, toRequest, validate,
  type RecipeFormState,
} from './recipeForm';

const recipe: Recipe = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Sunday Roast',
  status: 'Published',
  description: 'Feeds a table.',
  preparationTime: 20,
  cookingTime: 85,
  servings: 4,
  tags: ['roast'],
  ingredients: [
    { id: 'aaaaaaaa-0000-0000-0000-00000000000a', quantity: 2, unit: 'Tablespoon', name: 'butter', notes: 'cold' },
    { id: 'aaaaaaaa-0000-0000-0000-00000000000b', quantity: null, unit: null, name: '7 spice', notes: null },
    { id: 'aaaaaaaa-0000-0000-0000-00000000000c', quantity: 1.6, unit: 'Kilogram', name: 'chicken', notes: null },
  ],
  instructions: [
    { id: 'bbbbbbbb-0000-0000-0000-000000000001', text: 'Rub.', durationMinutes: null, ingredientIds: ['aaaaaaaa-0000-0000-0000-00000000000a', 'aaaaaaaa-0000-0000-0000-00000000000c'] },
    { id: 'bbbbbbbb-0000-0000-0000-000000000002', text: 'Roast.', durationMinutes: 85, ingredientIds: ['aaaaaaaa-0000-0000-0000-00000000000c'] },
  ],
};

const keyOf = (state: RecipeFormState, name: string) =>
  state.ingredients.find(row => row.text.includes(name))!.key;

describe('fromRecipe → toRequest', () => {
  it('sends an untouched recipe back unchanged — ids, stored values, references, tags', () => {
    expect(toRequest(fromRecipe(recipe))).toEqual({
      title: 'Sunday Roast',
      description: 'Feeds a table.',
      preparationTime: 20,
      cookingTime: 85,
      servings: 4,
      ingredients: recipe.ingredients.map(({ id, quantity, unit, name, notes }) => ({ id, quantity, unit, name, notes })),
      instructions: [
        { text: 'Rub.', durationMinutes: null, ingredientIndexes: [0, 2] },
        { text: 'Roast.', durationMinutes: 85, ingredientIndexes: [2] },
      ],
      tags: ['roast'],
    });
  });

  // "7 spice" with no quantity would re-parse as (7, null, "spice"): an untouched row must not be re-parsed.
  it('sends the stored value of an untouched row even when its line would parse differently', () => {
    const state = fromRecipe(recipe);
    expect(state.ingredients[1].text).toBe('7 spice');
    expect(toRequest(state).ingredients[1]).toEqual({ id: recipe.ingredients[1].id, quantity: null, unit: null, name: '7 spice', notes: null });
  });

  it('re-parses a row once its text changes, keeping its id', () => {
    const state = fromRecipe(recipe);
    const edited = formReducer(state, { type: 'setIngredient', key: state.ingredients[0].key, text: '3 tbsp butter' });
    expect(toRequest(edited).ingredients[0]).toEqual({ id: recipe.ingredients[0].id, quantity: 3, unit: 'Tablespoon', name: 'butter', notes: null });
  });
});

describe('step references', () => {
  it('are indexes into the array actually sent, after a row is inserted before them', () => {
    const state = fromRecipe(recipe);
    const inserted = formReducer(state, {
      type: 'addIngredient',
      after: keyOf(state, 'butter'),
      row: { ...emptyIngredient(), text: '1 lemon' },
    });
    const request = toRequest(inserted);
    expect(request.ingredients.map(i => i.name)).toEqual(['butter', 'lemon', '7 spice', 'chicken']);
    expect(request.instructions.map(s => s.ingredientIndexes)).toEqual([[0, 3], [3]]);
    expect(request.ingredients[1].id).toBeNull();
  });

  it('lose a removed ingredient in the same update', () => {
    const state = fromRecipe(recipe);
    const removed = formReducer(state, { type: 'removeIngredient', key: keyOf(state, 'chicken') });
    expect(removed.steps.map(s => s.ingredientKeys)).toEqual([[keyOf(state, 'butter')], []]);
    expect(toRequest(removed).instructions.map(s => s.ingredientIndexes)).toEqual([[0], []]);
  });

  it('skip blank rows, and a reference to a blank row is dropped', () => {
    const state = formReducer(emptyForm(), { type: 'setField', field: 'title', value: 'Toast' });
    const blankKey = state.ingredients[0].key;
    const stepKey = state.steps[0].key;
    const next = [
      { type: 'addIngredient', row: { ...emptyIngredient(), text: '2 slices bread' } },
      { type: 'setStep', key: stepKey, text: 'Toast it.' },
      { type: 'toggleStepIngredient', stepKey, ingredientKey: blankKey },
    ] as const;
    const filled = next.reduce(formReducer, state);
    const request = toRequest(filled);
    expect(request.ingredients).toEqual([{ id: null, quantity: 2, unit: 'Slice', name: 'bread', notes: null }]);
    expect(request.instructions).toEqual([{ text: 'Toast it.', durationMinutes: null, ingredientIndexes: [] }]);
  });
});

describe('formReducer', () => {
  it('toggles a step ingredient on and off', () => {
    const state = fromRecipe(recipe);
    const step = state.steps[1].key;
    const butter = keyOf(state, 'butter');
    const on = formReducer(state, { type: 'toggleStepIngredient', stepKey: step, ingredientKey: butter });
    expect(on.steps[1].ingredientKeys).toContain(butter);
    const off = formReducer(on, { type: 'toggleStepIngredient', stepKey: step, ingredientKey: butter });
    expect(off.steps[1].ingredientKeys).not.toContain(butter);
  });

  it('normalises tags and ignores blanks and duplicates', () => {
    const tagged = [' Roast ', 'roast', '  ', 'Feeds   A Table'].reduce(
      (state, tag) => formReducer(state, { type: 'addTag', tag }),
      emptyForm(),
    );
    expect(tagged.tags).toEqual(['roast', 'feeds a table']);
    expect(formReducer(tagged, { type: 'removeTag', tag: 'roast' }).tags).toEqual(['feeds a table']);
  });

  it('adds and removes steps', () => {
    const step = emptyStep();
    const added = formReducer(emptyForm(), { type: 'addStep', step });
    expect(added.steps).toHaveLength(2);
    expect(formReducer(added, { type: 'removeStep', key: step.key }).steps).toHaveLength(1);
  });
});

describe('validate', () => {
  const withTitle = formReducer(emptyForm(), { type: 'setField', field: 'title', value: 'Bare' });

  it('needs only a title for a draft', () => {
    expect(validate(withTitle, 'draft')).toEqual({});
    expect(validate(emptyForm(), 'draft')).toEqual({ title: ['Add a title'] });
  });

  it('lists every gap at once to publish', () => {
    expect(validate(withTitle, 'publish')).toEqual({
      description: ['Add a description to publish'],
      preparationTime: ['Add the hands-on time to publish'],
      cookingTime: ['Add the oven time to publish'],
      servings: ['Add servings to publish'],
      ingredients: ['Add at least one ingredient to publish'],
      instructions: ['Add at least one step to publish'],
    });
  });

  it('puts "both times zero" on the times group, not on one input', () => {
    const state = { ...fromRecipe(recipe), prep: '0', cook: '0' };
    expect(validate(state, 'publish')).toEqual({ times: ["Hands-on and oven time can't both be zero"] });
    expect(validate(state, 'draft')).toEqual({});
  });

  it.each([
    ['prep', '1440', 'preparationTime', 'Hands-on time is whole minutes, 0–1439'],
    ['prep', '1.5', 'preparationTime', 'Hands-on time is whole minutes, 0–1439'],
    ['cook', '-1', 'cookingTime', 'Oven time is whole minutes, 0–1439'],
    ['servings', '0', 'servings', 'Serves is a whole number from 1 to 999'],
    ['servings', '1000', 'servings', 'Serves is a whole number from 1 to 999'],
  ] as const)('rejects %s = %j even in a draft', (field, value, key, message) => {
    expect(validate({ ...withTitle, [field]: value }, 'draft')).toEqual({ [key]: [message] });
  });

  it('flags an unparseable ingredient line and a bad step timer', () => {
    const state = fromRecipe(recipe);
    const bad = [
      { type: 'setIngredient', key: state.ingredients[0].key, text: '0 eggs' },
      { type: 'setStep', key: state.steps[0].key, duration: '0' },
    ] as const;
    expect(validate(bad.reduce(formReducer, state), 'draft')).toEqual({
      ingredients: ['Fix the ingredient lines marked below'],
      instructions: ['A step timer is whole minutes, 1–1439'],
    });
  });

  it('caps the lists', () => {
    const many = { ...withTitle, ingredients: Array.from({ length: 51 }, () => ({ ...emptyIngredient(), text: '1 egg' })) };
    expect(validate(many, 'draft')).toEqual({ ingredients: ['Up to 50 ingredients'] });
    expect(hasErrors(validate(many, 'draft'))).toBe(true);
    expect(hasErrors({})).toBe(false);
  });
});

describe('fromServerErrors', () => {
  it('places known fields, the cross-field pair on the times group, and the rest on the form', () => {
    expect(fromServerErrors({
      fields: { title: ['t'], 'preparationTime,cookingTime': ['z'], instructions: ['i'], id: ['gone'] },
      form: ['offline'],
    })).toEqual({ title: ['t'], times: ['z'], instructions: ['i'], form: ['offline', 'gone'] });
  });
});
