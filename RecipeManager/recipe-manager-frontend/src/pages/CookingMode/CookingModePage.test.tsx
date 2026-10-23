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

const openCooking = async (path?: string, value: Recipe = recipe) => {
  resolveWith(value);
  const view = renderCooking(path);
  await screen.findByRole('heading', { level: 1, name: value.title });
  return view;
};

const next = () => fireEvent.click(screen.getByRole('button', { name: /Next step/ }));

describe('CookingModePage steps', () => {
  it('shows step 1 of 3 by default, with Back unavailable', async () => {
    await openCooking();
    expect(screen.getByRole('heading', { name: 'Step 1 of 3' })).toBeTruthy();
    expect(screen.getByText('Pat the chicken dry.')).toBeTruthy();
    expect(screen.getByRole('button', { name: /Back/ }).getAttribute('aria-disabled')).toBe('true');
  });

  it('advances with Next step and records the step in the URL without a history entry', async () => {
    const { router } = await openCooking();
    next();
    expect(screen.getByRole('heading', { name: 'Step 2 of 3' })).toBeTruthy();
    expect(router.state.location.search).toBe('?step=2');
    expect(router.state.historyAction).toBe('REPLACE');
  });

  it('opens at the step in the URL, clamped to the last one', async () => {
    await openCooking(`/recipes/${ID}/cook?step=99`);
    expect(screen.getByRole('heading', { name: 'Step 3 of 3' })).toBeTruthy();
  });

  it('keeps the servings in the URL when the step changes', async () => {
    const { router } = await openCooking(`/recipes/${ID}/cook?servings=6`);
    next();
    expect(new URLSearchParams(router.state.location.search).get('servings')).toBe('6');
  });

  it('shows Finish instead of Next step on the last step', async () => {
    await openCooking(`/recipes/${ID}/cook?step=3`);
    expect(screen.getByRole('button', { name: 'Finish' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: /Next step/ })).toBeNull();
  });

  it('does nothing on Back from step 1', async () => {
    await openCooking();
    fireEvent.click(screen.getByRole('button', { name: /Back/ }));
    expect(screen.getByRole('heading', { name: 'Step 1 of 3' })).toBeTruthy();
  });

  it('moves focus to the new step heading', async () => {
    await openCooking();
    next();
    expect(document.activeElement).toBe(screen.getByRole('heading', { name: 'Step 2 of 3' }));
  });

  it('changes step with the arrow keys', async () => {
    await openCooking();
    fireEvent.keyDown(window, { key: 'ArrowRight' });
    expect(screen.getByRole('heading', { name: 'Step 2 of 3' })).toBeTruthy();
    fireEvent.keyDown(window, { key: 'ArrowLeft' });
    expect(screen.getByRole('heading', { name: 'Step 1 of 3' })).toBeTruthy();
  });

  it('does not open the finish panel with ArrowRight on the last step', async () => {
    await openCooking(`/recipes/${ID}/cook?step=3`);
    fireEvent.keyDown(window, { key: 'ArrowRight' });
    expect(screen.queryByRole('heading', { name: 'Log this cook?' })).toBeNull();
  });

  it('links Close back to the recipe', async () => {
    await openCooking();
    expect(screen.getByRole('link', { name: 'Leave cooking mode' }).getAttribute('href')).toBe(`/recipes/${ID}`);
  });
});

describe('CookingModePage ingredients', () => {
  it('splits a by-step recipe into this step, already used, and also needed', async () => {
    await openCooking(`/recipes/${ID}/cook?step=3`);
    const forStep = screen.getByRole('list', { name: 'For this step' });
    const used = screen.getByRole('list', { name: 'Already used' });
    const alsoNeeded = screen.getByRole('list', { name: 'Also needed' });
    expect(forStep.textContent).toContain('new potatoes');
    expect(used.textContent).toContain('whole chicken');
    expect(used.textContent).toContain('lemons');
    expect(used.textContent).toContain(', used');
    expect(alsoNeeded.textContent).toContain('flaky salt');
  });

  it('shows the full list when no step references an ingredient', async () => {
    const unreferenced = { ...recipe, instructions: recipe.instructions.map(s => ({ ...s, ingredientIds: [] })) };
    await openCooking(undefined, unreferenced);
    const all = screen.getByRole('list', { name: 'Ingredients' });
    expect(all.querySelectorAll('li')).toHaveLength(4);
    expect(screen.queryByRole('list', { name: 'Already used' })).toBeNull();
  });

  it('scales quantities by the servings in the URL', async () => {
    await openCooking(`/recipes/${ID}/cook?step=2&servings=6`);
    expect(screen.getByRole('list', { name: 'For this step' }).textContent).toContain('3');
  });
});

const logCook = vi.mocked(recipeService.logCook);
const openFinish = async () => {
  const view = await openCooking(`/recipes/${ID}/cook?step=3`);
  fireEvent.click(screen.getByRole('button', { name: 'Finish' }));
  return view;
};

describe('CookingModePage finish', () => {
  it('asks before logging and focuses the question', async () => {
    await openFinish();
    expect(document.activeElement).toBe(screen.getByRole('heading', { name: 'Log this cook?' }));
    expect(logCook).not.toHaveBeenCalled();
  });

  it('closes the question with Escape', async () => {
    await openFinish();
    expect(screen.queryByRole('button', { name: 'Finish' })).toBeNull();
    fireEvent.keyDown(window, { key: 'Escape' });
    expect(screen.queryByRole('heading', { name: 'Log this cook?' })).toBeNull();
    expect(screen.getByRole('button', { name: 'Finish' })).toBeTruthy();
  });

  it('logs the cook once and returns to the recipe without a history entry', async () => {
    logCook.mockResolvedValue({ data: { id: 'c1', recipeId: ID, cookedAt: '2026-10-23T18:00:00Z' } } as AxiosResponse);
    const { router } = await openFinish();
    fireEvent.click(screen.getByRole('button', { name: 'Log cook' }));
    expect(await screen.findByText('detail page')).toBeTruthy();
    expect(logCook).toHaveBeenCalledTimes(1);
    expect(logCook).toHaveBeenCalledWith(ID, undefined);
    expect(router.state.historyAction).toBe('REPLACE');
  });

  it('logs one cook on a double tap', async () => {
    logCook.mockReturnValue(new Promise(() => undefined));
    await openFinish();
    const button = screen.getByRole('button', { name: 'Log cook' });
    fireEvent.click(button);
    fireEvent.click(button);
    // mutationFn runs after a microtask, so settle before counting.
    await act(() => new Promise(resolve => setTimeout(resolve, 0)));
    expect(logCook).toHaveBeenCalledTimes(1);
  });

  it('keeps the question open with an error when logging fails', async () => {
    logCook.mockRejectedValue(new Error('boom'));
    await openFinish();
    fireEvent.click(screen.getByRole('button', { name: 'Log cook' }));
    expect((await screen.findByRole('alert')).textContent).toBe("Couldn't log this cook. Try again.");
    expect((screen.getByRole('button', { name: 'Log cook' }) as HTMLButtonElement).disabled).toBe(false);
  });

  it('returns to the recipe without logging on Not now', async () => {
    const { router } = await openFinish();
    fireEvent.click(screen.getByRole('button', { name: 'Not now' }));
    expect(await screen.findByText('detail page')).toBeTruthy();
    expect(logCook).not.toHaveBeenCalled();
    expect(router.state.historyAction).toBe('REPLACE');
  });

  it('ignores the arrow keys while the question is open', async () => {
    await openFinish();
    fireEvent.keyDown(window, { key: 'ArrowLeft' });
    expect(screen.getByRole('heading', { name: 'Log this cook?' })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Step 3 of 3' })).toBeTruthy();
  });
});
