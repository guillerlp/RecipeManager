import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { AxiosError, type AxiosResponse } from 'axios';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { UnitsProvider } from '@/contexts';
import { recipeService } from '@/services';
import type { Recipe } from '@/types';
import { CookingModePage } from './CookingModePage';

vi.mock('@/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@/services')>()),
  recipeService: { getRecipeById: vi.fn(), logCook: vi.fn() },
}));

const getRecipeById = vi.mocked(recipeService.getRecipeById);
const ID = '11111111-1111-1111-1111-111111111111';

const recipe = {
  id: ID,
  title: 'Sunday Lemon Roast Chicken',
  status: 'Published',
  description: null,
  preparationTime: 20,
  cookingTime: 85,
  servings: 4,
  tags: [],
  cookCount: 0,
  ingredients: [
    { id: 'i1', quantity: 1.6, unit: 'Kilogram', name: 'whole chicken', notes: null },
    { id: 'i2', quantity: 2, unit: null, name: 'lemons', notes: 'one halved' },
    { id: 'i3', quantity: 600, unit: 'Gram', name: 'new potatoes', notes: 'halved' },
    { id: 'i4', quantity: null, unit: null, name: 'flaky salt', notes: null },
  ],
  instructions: [
    { id: 's1', text: 'Pat the chicken dry.', durationMinutes: null, ingredientIds: ['i1'] },
    { id: 's2', text: 'Push the lemon into the cavity.', durationMinutes: null, ingredientIds: ['i2'] },
    { id: 's3', text: 'Toss the potatoes in the tin.', durationMinutes: 85, ingredientIds: ['i3'] },
  ],
} satisfies Recipe;

const notFound = () =>
  new AxiosError('Not Found', 'ERR_BAD_REQUEST', undefined, undefined, { status: 404 } as AxiosResponse);

const resolveWith = (value: Recipe) =>
  getRecipeById.mockResolvedValue({ data: value } as AxiosResponse<Recipe>);

const renderCooking = (path = `/recipes/${ID}/cook`) => {
  const router = createMemoryRouter(
    [
      { path: '/recipes/:id/cook', element: <CookingModePage /> },
      { path: '/recipes/:id', element: <p>detail page</p> },
      { path: '/recipes', element: <p>recipes page</p> },
    ],
    { initialEntries: [path] },
  );
  const view = render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <UnitsProvider>
        <RouterProvider router={router} />
      </UnitsProvider>
    </QueryClientProvider>,
  );
  return { router, ...view };
};

beforeEach(() => {
  getRecipeById.mockReset();
  vi.mocked(recipeService.logCook).mockReset();
  localStorage.clear();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('CookingModePage states', () => {
  it('shows loading while the recipe is fetched', () => {
    getRecipeById.mockReturnValue(new Promise(() => undefined));
    renderCooking();
    expect(screen.getByText('Loading recipe…')).toBeTruthy();
  });

  it('shows not-found for an id that is not a recipe id', () => {
    renderCooking('/recipes/nope/cook');
    expect(screen.getByText("We couldn't find that recipe.")).toBeTruthy();
    expect(screen.getByRole('link', { name: 'All recipes' }).getAttribute('href')).toBe('/recipes');
    expect(getRecipeById).not.toHaveBeenCalled();
  });

  it('shows not-found for a 404', async () => {
    getRecipeById.mockRejectedValue(notFound());
    renderCooking();
    expect(await screen.findByText("We couldn't find that recipe.")).toBeTruthy();
  });

  it('shows an error with a Retry that recovers', async () => {
    vi.useFakeTimers();
    getRecipeById.mockRejectedValue(new Error('connect ECONNREFUSED 127.0.0.1:5432'));
    renderCooking();

    // useRecipe's own retry: 2 with the default backoff (1 s, then 2 s).
    await act(() => vi.advanceTimersByTimeAsync(5_000));
    expect(screen.getByText("Couldn't load this recipe.")).toBeTruthy();
    expect(screen.queryByText(/ECONNREFUSED/)).toBeNull();

    vi.useRealTimers();
    resolveWith(recipe);
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByRole('heading', { level: 1, name: recipe.title })).toBeTruthy();
  });

  it('explains a recipe with no steps and links back to it', async () => {
    resolveWith({ ...recipe, instructions: [] });
    renderCooking();
    expect(await screen.findByText('This recipe has no steps yet.')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Back to the recipe' }).getAttribute('href')).toBe(`/recipes/${ID}`);
  });

  it('renders under the cooking palette', async () => {
    resolveWith(recipe);
    const { container } = renderCooking();
    await screen.findByRole('heading', { level: 1, name: recipe.title });
    expect(container.querySelector('[data-palette="cooking"]')).not.toBeNull();
  });
});
