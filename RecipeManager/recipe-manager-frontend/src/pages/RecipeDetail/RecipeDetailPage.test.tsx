import { QueryClient, QueryClientProvider, onlineManager } from '@tanstack/react-query';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { AxiosError, type AxiosResponse } from 'axios';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { UnitsProvider } from '@/contexts';
import { recipeService } from '@/services';
import type { Recipe } from '@/types';
import { RecipeDetailPage } from './RecipeDetailPage';

// Keep the real isNotFoundError; replace only the network call.
vi.mock('@/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@/services')>()),
  recipeService: { getRecipeById: vi.fn() },
}));

const getRecipeById = vi.mocked(recipeService.getRecipeById);
const ID = '11111111-1111-1111-1111-111111111111';

const recipe: Recipe = {
  id: ID,
  title: 'Sunday Lemon Roast Chicken',
  description: 'The one that makes the whole flat smell like Sunday.',
  preparationTime: 20,
  cookingTime: 85,
  servings: 4,
  ingredients: [
    { id: 'i1', quantity: 1.6, unit: 'Kilogram', name: 'whole chicken', notes: null },
    { id: 'i2', quantity: 2, unit: null, name: 'lemons', notes: 'one halved' },
    { id: 'i3', quantity: null, unit: null, name: 'black pepper', notes: null },
  ],
  instructions: [
    { id: 's1', text: 'Heat the oven to 200°C fan.', durationMinutes: null, ingredientIds: [] },
    { id: 's2', text: 'Roast for 1h 25min.', durationMinutes: 85, ingredientIds: ['i1'] },
  ],
};

const notFound = () =>
  new AxiosError('Not Found', 'ERR_BAD_REQUEST', undefined, undefined, { status: 404 } as AxiosResponse);

const renderPage = (path = `/recipes/${ID}`) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <UnitsProvider>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/recipes/:id" element={<RecipeDetailPage />} />
          </Routes>
        </MemoryRouter>
      </UnitsProvider>
    </QueryClientProvider>,
  );

const originalMatchMedia = window.matchMedia;
const pretendMobile = () => {
  window.matchMedia = (query: string) =>
    ({ matches: true, media: query, addEventListener: () => undefined, removeEventListener: () => undefined }) as unknown as MediaQueryList;
};

beforeEach(() => {
  getRecipeById.mockReset();
  localStorage.clear();
});

afterEach(() => {
  vi.useRealTimers();
  window.matchMedia = originalMatchMedia;
  onlineManager.setOnline(true);
});

describe('RecipeDetailPage states', () => {
  it('shows the loading state while the request is pending', () => {
    getRecipeById.mockReturnValue(new Promise<AxiosResponse<Recipe>>(() => undefined));
    renderPage();

    expect(screen.getByText('Loading recipe…')).toBeTruthy();
  });

  it('says it is offline instead of loading forever when the query is paused', () => {
    onlineManager.setOnline(false);
    getRecipeById.mockResolvedValue({ data: recipe } as AxiosResponse<Recipe>);
    renderPage();

    expect(screen.getByText('You appear to be offline. The recipe will load when you reconnect.')).toBeTruthy();
    expect(getRecipeById).not.toHaveBeenCalled();
  });

  it('shows "Recipe not found" on a 404, without retrying', async () => {
    getRecipeById.mockRejectedValue(notFound());
    renderPage();

    expect(await screen.findByRole('heading', { name: 'Recipe not found' })).toBeTruthy();
    expect(screen.getByRole('link', { name: '← All recipes' }).getAttribute('href')).toBe('/recipes');
    expect(getRecipeById).toHaveBeenCalledTimes(1);
  });

  // React Router decodes %2F in a param, so without a guard this id would make the SPA request
  // /api/Recipes/../Recipes — the list endpoint — and crash rendering an array as one recipe.
  it.each(['/recipes/..%2FRecipes', '/recipes/%2E', '/recipes/not-a-guid'])(
    'treats %s as not found without calling the API',
    path => {
      renderPage(path);

      expect(screen.getByRole('heading', { name: 'Recipe not found' })).toBeTruthy();
      expect(getRecipeById).not.toHaveBeenCalled();
    },
  );

  it('shows a generic error and recovers through Retry', async () => {
    vi.useFakeTimers();
    getRecipeById.mockRejectedValue(new Error('connect ECONNREFUSED 127.0.0.1:5432'));
    renderPage();

    // retry: 2 with the default backoff (1 s, then 2 s).
    await act(() => vi.advanceTimersByTimeAsync(5_000));
    expect(screen.getByText("Couldn't load this recipe.")).toBeTruthy();
    // The server's message is not shown: it can leak infrastructure detail (SEC-05).
    expect(screen.queryByText(/ECONNREFUSED/)).toBeNull();
    expect(getRecipeById).toHaveBeenCalledTimes(3);

    vi.useRealTimers();
    getRecipeById.mockResolvedValue({ data: recipe } as AxiosResponse<Recipe>);
    await act(async () => {
      screen.getByRole('button', { name: 'Retry' }).click();
      await Promise.resolve();
    });

    expect(await screen.findByRole('heading', { level: 1, name: recipe.title })).toBeTruthy();
  });
});

describe('RecipeDetailPage populated', () => {
  beforeEach(() => {
    getRecipeById.mockResolvedValue({ data: recipe } as AxiosResponse<Recipe>);
  });

  it('renders the recipe with its stats, method and ingredients side by side on desktop', async () => {
    renderPage();

    expect(await screen.findByRole('heading', { level: 1, name: recipe.title })).toBeTruthy();
    expect(screen.getByText(recipe.description)).toBeTruthy();
    expect(screen.getByText('1h 45min').getAttribute('datetime')).toBe('PT1H45M');
    expect(screen.getByText('20 min')).toBeTruthy();
    expect(screen.getByText('1h 25min')).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Method' })).toBeTruthy();
    expect(screen.getByRole('complementary', { name: 'Ingredients' })).toBeTruthy();
    expect(screen.queryByRole('tablist')).toBeNull();
  });

  it('omits a time stat whose value is zero', async () => {
    getRecipeById.mockResolvedValue({ data: { ...recipe, preparationTime: 0 } } as AxiosResponse<Recipe>);
    renderPage();

    await screen.findByRole('heading', { level: 1, name: recipe.title });
    expect(screen.queryByText('Hands on')).toBeNull();
    expect(screen.getByText('Cooking')).toBeTruthy();
  });

  it('rescales ingredients and the Serves stat together', async () => {
    renderPage();
    await screen.findByRole('heading', { level: 1, name: recipe.title });

    for (let i = 0; i < 4; i++) fireEvent.click(screen.getByRole('button', { name: 'Increase servings' }));

    expect(screen.getByText('3.2 kg')).toBeTruthy();
    expect(screen.getByText('4')).toBeTruthy(); // lemons: 2 for 4 → 4 for 8
    expect(screen.getByTestId('serves').textContent).toBe('8');
  });

  it('uses tabs on mobile', async () => {
    pretendMobile();
    renderPage();
    await screen.findByRole('heading', { level: 1, name: recipe.title });

    expect(screen.getByRole('tab', { name: 'Ingredients' }).getAttribute('aria-selected')).toBe('true');
    fireEvent.click(screen.getByRole('tab', { name: 'Method' }));
    expect(screen.getByRole('tabpanel').textContent).toContain('Heat the oven to 200°C fan.');
  });

  it('prints through window.print', async () => {
    const print = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    renderPage();
    await screen.findByRole('heading', { level: 1, name: recipe.title });

    fireEvent.click(screen.getByRole('button', { name: 'Print' }));

    expect(print).toHaveBeenCalledTimes(1);
    print.mockRestore();
  });

  it('shows quantities in the unit system chosen in Settings', async () => {
    localStorage.setItem('units', 'imperial');
    renderPage();

    expect(await screen.findByText('3.5 lb')).toBeTruthy();
  });
});
