import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen } from '@testing-library/react';
import type { AxiosResponse } from 'axios';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { recipeService } from '@/services';
import type { Recipe } from '@/types';
import { RecipePage } from './RecipePage';

vi.mock('@/services', () => ({
  recipeService: { getAllRecipes: vi.fn() },
}));

const recipe = (id: string, title: string, tags: string[]): Recipe => ({
  id, title, tags, status: 'Published', description: '', preparationTime: 10, cookingTime: 20, servings: 2,
  ingredients: [], instructions: [],
});

const renderAt = (path: string) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter initialEntries={[path]}>
        <RecipePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );

const titles = async () =>
  (await screen.findAllByRole('heading', { level: 3 })).map(heading => heading.textContent);

beforeEach(() => {
  vi.mocked(recipeService.getAllRecipes).mockResolvedValue({
    data: [
      recipe('11111111-1111-1111-1111-111111111111', 'Roast Chicken', ['roast', 'feeds a table']),
      recipe('22222222-2222-2222-2222-222222222222', 'Pancakes', ['breakfast']),
    ],
  } as AxiosResponse<Recipe[]>);
});

describe('RecipePage tag filter', () => {
  it('filters by ?tag=, normalising a hand-typed value the way the server normalises tags', async () => {
    renderAt('/recipes?tag=%20Feeds%20%20A%20Table%20');

    expect(await titles()).toEqual(['Roast Chicken']);
    expect(screen.getByRole('button', { name: 'Clear tag filter: feeds a table' })).toBeTruthy();
  });

  it('decodes an encoded tag', async () => {
    renderAt('/recipes?tag=a%26b');

    expect(await screen.findByText('No recipes tagged "a&b".')).toBeTruthy();
  });

  it('clears the filter and lists every recipe again', async () => {
    renderAt('/recipes?tag=roast');
    expect(await titles()).toEqual(['Roast Chicken']);

    fireEvent.click(screen.getByRole('button', { name: 'Clear tag filter: roast' }));

    expect(await titles()).toEqual(['Roast Chicken', 'Pancakes']);
    expect(screen.queryByRole('button', { name: /Clear tag filter/ })).toBeNull();
  });

  it('moves focus to the search field when the filter is cleared, instead of dropping it to the page', async () => {
    renderAt('/recipes?tag=roast');
    await titles();

    fireEvent.click(screen.getByRole('button', { name: 'Clear tag filter: roast' }));

    expect(document.activeElement).toBe(screen.getByRole('textbox', { name: 'Search recipes' }));
  });

  it('treats an all-whitespace ?tag= as no filter', async () => {
    renderAt('/recipes?tag=%20%20');

    expect(await titles()).toEqual(['Roast Chicken', 'Pancakes']);
    expect(screen.queryByRole('button', { name: /Clear tag filter/ })).toBeNull();
  });

  it('shows no filter control without ?tag=', async () => {
    renderAt('/recipes');

    expect(await titles()).toEqual(['Roast Chicken', 'Pancakes']);
    expect(screen.queryByRole('button', { name: /Clear tag filter/ })).toBeNull();
  });
});
