import { fireEvent, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import type { Ingredient } from '@/types';
import { IngredientRail } from './IngredientRail';

let nextId = 0;
const ingredient = (overrides: Partial<Ingredient>): Ingredient => ({
  id: `00000000-0000-0000-0000-${String(++nextId).padStart(12, '0')}`,
  quantity: null,
  unit: null,
  name: 'x',
  notes: null,
  ...overrides,
});

const ingredients: Ingredient[] = [
  ingredient({ quantity: 1.6, unit: 'Kilogram', name: 'whole chicken' }),
  ingredient({ quantity: 2, unit: null, name: 'lemons', notes: 'one halved' }),
  ingredient({ quantity: 1, unit: 'Teaspoon', name: 'flaky salt' }),
  ingredient({ quantity: null, unit: null, name: 'black pepper', notes: 'to taste' }),
];

// Owns the state the way RecipeDetailPage will, so the stepper actually drives the amounts.
const Harness = () => {
  const [servings, setServings] = useState(4);
  return <IngredientRail ingredients={ingredients} writtenServings={4} servings={servings} onServingsChange={setServings} />;
};

const rowTexts = () => screen.getAllByRole('listitem').map(item => item.textContent);

describe('IngredientRail', () => {
  it('shows the written amounts at the written servings, with spoken forms for screen readers', () => {
    render(<Harness />);

    expect(screen.getByRole('list').getAttribute('role')).toBe('list');
    expect(screen.getByText('1.6 kg').getAttribute('aria-hidden')).toBe('true');
    expect(screen.getByText('1.6 kilograms')).toBeTruthy();
    expect(screen.getByText('Scaled for 4. Change the number and every quantity follows.')).toBeTruthy();
  });

  it('rescales every quantity, including a unitless count, and leaves "to taste" alone', () => {
    render(<Harness />);

    fireEvent.click(screen.getByRole('button', { name: 'Increase servings' }));

    expect(screen.getByText('2 kg')).toBeTruthy();
    expect(screen.getByText('2.5')).toBeTruthy();
    expect(screen.getByText('1¼ tsp')).toBeTruthy();
    const rows = rowTexts();
    expect(rows[rows.length - 1]).toBe('black pepper, to taste');
    expect(screen.getByText('Scaled for 5. Change the number and every quantity follows.')).toBeTruthy();
  });

  it('returns to the written amounts after stepping up and back down', () => {
    render(<Harness />);
    const before = rowTexts();

    fireEvent.click(screen.getByRole('button', { name: 'Increase servings' }));
    fireEvent.click(screen.getByRole('button', { name: 'Increase servings' }));
    fireEvent.click(screen.getByRole('button', { name: 'Increase servings' }));
    fireEvent.click(screen.getByRole('button', { name: 'Decrease servings' }));
    fireEvent.click(screen.getByRole('button', { name: 'Decrease servings' }));
    fireEvent.click(screen.getByRole('button', { name: 'Decrease servings' }));

    expect(rowTexts()).toEqual(before);
  });
});
