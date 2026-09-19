import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import type { AxiosResponse } from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { recipeService } from '@/services';
import type { Recipe } from '@/types';
import { RecipeList } from './RecipeList';

vi.mock('@/services', () => ({
  recipeService: { getAllRecipes: vi.fn() },
}));

const getAllRecipes = vi.mocked(recipeService.getAllRecipes);

const makeRecipe = (overrides: Partial<Recipe>): Recipe => ({
  id: '00000000-0000-0000-0000-000000000000',
  title: 'Untitled',
  description: '',
  preparationTime: 10,
  cookingTime: 20,
  servings: 2,
  ingredients: [],
  instructions: [],
  ...overrides,
});

const recipes: Recipe[] = [
  makeRecipe({ id: '11111111-1111-1111-1111-111111111111', title: 'Tomato Soup', description: 'Warm and simple', ingredients: ['tomato', 'basil'], preparationTime: 90 }),
  makeRecipe({ id: '22222222-2222-2222-2222-222222222222', title: 'Pancakes', description: 'Fluffy breakfast', ingredients: ['flour', 'milk'] }),
  makeRecipe({ id: '33333333-3333-3333-3333-333333333333', title: 'Green Salad', description: 'Crunchy side', ingredients: ['lettuce', 'cucumber'] }),
];

// useRecipes reads only `data`; the rest of the AxiosResponse is irrelevant to the component.
const respondWith = (data: Recipe[]) =>
  getAllRecipes.mockResolvedValue({ data } as AxiosResponse<Recipe[]>);

// A fresh QueryClient per render: a shared one would serve the previous test's cached list.
const renderList = (searchQuery?: string) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <RecipeList searchQuery={searchQuery} />
    </QueryClientProvider>,
  );

const cardTitles = async () =>
  (await screen.findAllByRole('heading', { level: 3 })).map(heading => heading.textContent);

beforeEach(() => {
  getAllRecipes.mockReset();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('RecipeList filtering', () => {
  it.each<{ scenario: string; query: string; expected: string[] }>([
    { scenario: 'an empty query returns every recipe', query: '', expected: ['Tomato Soup', 'Pancakes', 'Green Salad'] },
    { scenario: 'matches on title', query: 'pancakes', expected: ['Pancakes'] },
    { scenario: 'matches on description', query: 'crunchy', expected: ['Green Salad'] },
    { scenario: 'matches on an ingredient', query: 'basil', expected: ['Tomato Soup'] },
    { scenario: 'is case-insensitive', query: 'TOMATO', expected: ['Tomato Soup'] },
    { scenario: 'trims surrounding whitespace', query: '  flour  ', expected: ['Pancakes'] },
  ])('$scenario', async ({ query, expected }) => {
    respondWith(recipes);
    renderList(query);

    expect(await cardTitles()).toEqual(expected);
  });
});

describe('RecipeList states', () => {
  it('shows the loading state while the request is pending', () => {
    getAllRecipes.mockReturnValue(new Promise<AxiosResponse<Recipe[]>>(() => undefined));
    renderList();

    expect(screen.getByText('Loading…')).toBeTruthy();
  });

  it('shows the error message once the retries are exhausted', async () => {
    vi.useFakeTimers();
    getAllRecipes.mockRejectedValue(new Error('Network down'));
    renderList();

    // useRecipes hard-codes `retry: 2`, and a per-query option beats any QueryClient default,
    // so the error only surfaces after the real backoff (1 s, then 2 s). Advance past it.
    await act(() => vi.advanceTimersByTimeAsync(5_000));

    expect(screen.getByText(/Network down/)).toBeTruthy();
    expect(getAllRecipes).toHaveBeenCalledTimes(3);
  });

  it('shows "No recipes available" when there are no recipes and no query', async () => {
    respondWith([]);
    renderList();

    expect(await screen.findByText('No recipes available')).toBeTruthy();
  });

  it('shows "No recipes found" when recipes exist but none match the query', async () => {
    respondWith(recipes);
    renderList('zzz');

    expect(await screen.findByText('No recipes found')).toBeTruthy();
  });

  it('renders one card per recipe, with formatted durations', async () => {
    respondWith(recipes);
    renderList();

    expect(await cardTitles()).toHaveLength(3);
    // Guards the Task 1 extraction end to end: RecipeCard still formats through the helpers.
    const prepTime = screen.getByText('1h 30min');
    expect(prepTime.getAttribute('datetime')).toBe('PT1H30M');
  });
});
