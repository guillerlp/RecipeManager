import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { AxiosError, type AxiosResponse } from 'axios';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { recipeService } from '@/services';
import type { Recipe } from '@/types';
import { RecipeFormPage } from './RecipeFormPage';

// Keep the real readServerErrors and isNotFoundError; replace only the network calls.
vi.mock('@/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@/services')>()),
  recipeService: {
    getRecipeById: vi.fn(), getAllRecipes: vi.fn(), createRecipe: vi.fn(), updateRecipe: vi.fn(),
    publishRecipe: vi.fn(), unpublishRecipe: vi.fn(), deleteRecipe: vi.fn(),
  },
}));

const service = vi.mocked(recipeService);
const ID = '11111111-1111-1111-1111-111111111111';

const draft: Recipe = {
  id: ID, title: 'Half a roast', status: 'Draft', description: null, preparationTime: null, cookingTime: null,
  servings: null, ingredients: [], instructions: [], tags: [],
};

const published: Recipe = {
  id: ID, title: 'Sunday Roast', status: 'Published', description: 'Feeds a table.', preparationTime: 20,
  cookingTime: 85, servings: 4, tags: ['roast'],
  ingredients: [
    { id: 'aaaaaaaa-0000-0000-0000-000000000001', quantity: 0.333, unit: 'Cup', name: 'stock', notes: null },
    { id: 'aaaaaaaa-0000-0000-0000-000000000002', quantity: null, unit: null, name: '7 spice', notes: 'a pinch' },
    { id: 'aaaaaaaa-0000-0000-0000-000000000003', quantity: 1.6, unit: 'Kilogram', name: 'chicken', notes: null },
  ],
  instructions: [
    { id: 'bbbbbbbb-0000-0000-0000-000000000001', text: 'Roast it.', durationMinutes: 85, ingredientIds: ['aaaaaaaa-0000-0000-0000-000000000003'] },
  ],
};

const ok = <T,>(data: T) => ({ data }) as AxiosResponse<T>;
const httpError = (status: number, data: unknown) =>
  new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, { status, data } as AxiosResponse);

const renderAt = (path: string) => {
  const router = createMemoryRouter(
    [
      { path: '/recipes/new', element: <RecipeFormPage /> },
      { path: '/recipes/:id/edit', element: <RecipeFormPage /> },
      { path: '/recipes/:id', element: <p>Detail page</p> },
      { path: '/recipes', element: <p>List page</p> },
    ],
    { initialEntries: [path] },
  );
  render(
    <QueryClientProvider client={new QueryClient()}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return router;
};

const typeInto = (label: string, value: string) =>
  fireEvent.change(screen.getByLabelText(label), { target: { value } });

beforeEach(() => {
  vi.resetAllMocks();
});

describe('RecipeFormPage — new recipe', () => {
  it('saves a draft and replaces the URL with its edit screen', async () => {
    service.createRecipe.mockResolvedValue(ok(draft));
    service.getRecipeById.mockResolvedValue(ok(draft));
    const router = renderAt('/recipes/new');

    typeInto('Recipe title', 'Half a roast');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));

    expect(await screen.findByText(/^Draft saved/)).toBeTruthy();
    expect(router.state.location.pathname).toBe(`/recipes/${ID}/edit`);
    expect(router.state.historyAction).toBe('REPLACE');
    expect(service.createRecipe).toHaveBeenCalledWith({
      title: 'Half a roast', description: null, preparationTime: null, cookingTime: null, servings: null,
      ingredients: [], instructions: [], tags: [], status: 'Draft',
    });
  });

  it('blocks Publish without a request, lists every gap, and moves focus to the list', () => {
    renderAt('/recipes/new');
    typeInto('Recipe title', 'Bare');
    fireEvent.click(screen.getByRole('button', { name: 'Publish' }));

    const summary = screen.getByRole('region', { name: 'Fix 6 things before publishing' });
    expect(document.activeElement).toBe(summary);
    for (const message of [
      'Add a description to publish', 'Add the hands-on time to publish', 'Add the oven time to publish',
      'Add servings to publish', 'Add at least one ingredient to publish', 'Add at least one step to publish',
    ]) {
      expect(within(summary).getByText(message)).toBeTruthy();
    }
    expect(service.createRecipe).not.toHaveBeenCalled();
  });

  it('a summary link focuses its field', () => {
    renderAt('/recipes/new');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    fireEvent.click(screen.getByRole('link', { name: 'Add a title' }));
    expect(document.activeElement).toBe(screen.getByLabelText('Recipe title'));
  });

  it('a double click creates one recipe', async () => {
    service.createRecipe.mockResolvedValue(ok(draft));
    service.getRecipeById.mockResolvedValue(ok(draft));
    renderAt('/recipes/new');
    typeInto('Recipe title', 'Twice');
    const save = screen.getByRole('button', { name: 'Save draft' });
    fireEvent.click(save);
    fireEvent.click(save);
    await screen.findByText(/^Draft saved/);
    expect(service.createRecipe).toHaveBeenCalledTimes(1);
  });

  it('Enter in an ingredient line adds a line below it and focuses it, without saving', () => {
    renderAt('/recipes/new');
    const first = screen.getByLabelText('Ingredient 1');
    fireEvent.change(first, { target: { value: '2 tbsp butter, cold' } });
    expect(screen.getByText('→ 2 tbsp · butter, cold')).toBeTruthy();

    fireEvent.keyDown(first, { key: 'Enter' });

    expect(document.activeElement).toBe(screen.getByLabelText('Ingredient 2'));
    expect(service.createRecipe).not.toHaveBeenCalled();
  });

  it('shows a parse failure inline on its line', () => {
    renderAt('/recipes/new');
    const line = screen.getByLabelText('Ingredient 1');
    fireEvent.change(line, { target: { value: '0 eggs' } });
    expect(line.getAttribute('aria-invalid')).toBe('true');
    expect(screen.getByText('Quantity must be more than zero')).toBeTruthy();
  });

  it('a step can name the ingredients it uses, and the request carries their indexes', async () => {
    service.createRecipe.mockResolvedValue(ok(draft));
    service.getRecipeById.mockResolvedValue(ok(draft));
    renderAt('/recipes/new');
    typeInto('Recipe title', 'Toast');
    fireEvent.change(screen.getByLabelText('Ingredient 1'), { target: { value: '2 slices bread' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add ingredient' }));
    fireEvent.change(screen.getByLabelText('Ingredient 2'), { target: { value: 'butter' } });
    typeInto('Step 1', 'Butter the toast.');
    fireEvent.click(within(screen.getByRole('group', { name: 'Ingredients used in step 1' })).getByLabelText('butter'));

    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await screen.findByText(/^Draft saved/);

    expect(service.createRecipe.mock.calls[0][0].instructions).toEqual([
      { text: 'Butter the toast.', durationMinutes: null, ingredientIndexes: [1] },
    ]);
  });

  it('commits a tag on Enter or comma, and names its remove button', () => {
    renderAt('/recipes/new');
    const input = screen.getByLabelText('Add a tag');
    fireEvent.change(input, { target: { value: 'Roast' } });
    fireEvent.keyDown(input, { key: 'Enter' });
    fireEvent.change(input, { target: { value: 'weeknight' } });
    fireEvent.keyDown(input, { key: ',' });
    expect(screen.getByRole('button', { name: 'Remove tag roast' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Remove tag weeknight' })).toBeTruthy();
  });

  it('keeps the edits and shows a generic message when the network fails', async () => {
    service.createRecipe.mockRejectedValue(new AxiosError('Network Error', 'ERR_NETWORK'));
    renderAt('/recipes/new');
    typeInto('Recipe title', 'Offline soup');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));

    const summary = await screen.findByRole('region', { name: 'Fix 1 thing before saving' });
    expect(within(summary).getByText("Couldn't save. Check your connection and try again.")).toBeTruthy();
    expect(screen.getByLabelText<HTMLInputElement>('Recipe title').value).toBe('Offline soup');
  });
});

describe('RecipeFormPage — existing recipe', () => {
  it('sends an untouched recipe back unchanged', async () => {
    service.getRecipeById.mockResolvedValue(ok(published));
    service.updateRecipe.mockResolvedValue(ok(undefined));
    renderAt(`/recipes/${ID}/edit`);

    fireEvent.click(await screen.findByRole('button', { name: 'Save' }));
    await screen.findByText(/^Saved/);

    expect(service.updateRecipe).toHaveBeenCalledWith(ID, {
      title: 'Sunday Roast', description: 'Feeds a table.', preparationTime: 20, cookingTime: 85, servings: 4,
      ingredients: published.ingredients.map(({ id, quantity, unit, name, notes }) => ({ id, quantity, unit, name, notes })),
      instructions: [{ text: 'Roast it.', durationMinutes: 85, ingredientIndexes: [2] }],
      tags: ['roast'],
    });
  });

  it('publishes a draft by saving first; a refused publish leaves it saved with every gap marked', async () => {
    service.getRecipeById.mockResolvedValue(ok({ ...published, status: 'Draft' }));
    service.updateRecipe.mockResolvedValue(ok(undefined));
    service.publishRecipe.mockRejectedValue(httpError(422, {
      status: 422,
      errors: [
        { message: 'Description is required to publish', field: 'description', code: 422 },
        { message: 'Preparation and cooking time cannot both be zero', field: 'preparationTime,cookingTime', code: 422 },
      ],
    }));
    renderAt(`/recipes/${ID}/edit`);

    fireEvent.click(await screen.findByRole('button', { name: 'Publish' }));

    const summary = await screen.findByRole('region', { name: 'Fix 2 things before publishing' });
    expect(within(summary).getByText('Description is required to publish')).toBeTruthy();
    // Once in the summary and once under the times group — not under each time input.
    expect(screen.getAllByText('Preparation and cooking time cannot both be zero')).toHaveLength(2);
    expect(screen.getByText(/^Draft saved/)).toBeTruthy();
    expect(service.updateRecipe.mock.invocationCallOrder[0])
      .toBeLessThan(service.publishRecipe.mock.invocationCallOrder[0]);
  });

  it('unpublishes before saving, so the edit is checked as a draft', async () => {
    service.getRecipeById.mockResolvedValue(ok(published));
    service.unpublishRecipe.mockResolvedValue(ok(undefined));
    service.updateRecipe.mockResolvedValue(ok(undefined));
    renderAt(`/recipes/${ID}/edit`);

    const unpublishButton = await screen.findByRole('button', { name: 'Unpublish' });
    typeInto('Description', '');
    fireEvent.click(unpublishButton);
    await screen.findByText(/^Saved/);

    expect(service.unpublishRecipe.mock.invocationCallOrder[0])
      .toBeLessThan(service.updateRecipe.mock.invocationCallOrder[0]);
  });

  it('deletes after confirmation and lands on the list', async () => {
    service.getRecipeById.mockResolvedValue(ok(published));
    service.deleteRecipe.mockResolvedValue(ok(undefined));
    renderAt(`/recipes/${ID}/edit`);

    fireEvent.click(await screen.findByRole('button', { name: 'Delete recipe' }));
    const dialog = screen.getByRole('dialog', { name: 'Delete Sunday Roast?' });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(await screen.findByText('List page')).toBeTruthy();
    expect(service.deleteRecipe).toHaveBeenCalledWith(ID);
  });

  it('cancelling delete sends nothing and returns focus to Delete recipe', async () => {
    service.getRecipeById.mockResolvedValue(ok(published));
    renderAt(`/recipes/${ID}/edit`);

    const opener = await screen.findByRole('button', { name: 'Delete recipe' });
    fireEvent.click(opener);
    // Dialogs are always found by name: PR 2 adds a second one (the leave guard).
    const dialog = screen.getByRole('dialog', { name: 'Delete Sunday Roast?' });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }));

    expect(service.deleteRecipe).not.toHaveBeenCalled();
    expect(dialog.hasAttribute('open')).toBe(false);
    expect(document.activeElement).toBe(opener);
  });

  it('says so when the recipe does not exist', async () => {
    service.getRecipeById.mockRejectedValue(httpError(404, {}));
    renderAt(`/recipes/${ID}/edit`);
    expect(await screen.findByRole('heading', { name: 'Recipe not found' })).toBeTruthy();
  });
});
