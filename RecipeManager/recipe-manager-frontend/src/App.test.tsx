// src/App.test.tsx

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import type { AxiosResponse } from 'axios';
import { describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import App from './App';
import { NotFoundPage } from '@/pages/NotFound';
import { recipeService } from '@/services';
import type { Recipe } from '@/types';

vi.mock('@/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@/services')>()),
  recipeService: { getAllRecipes: vi.fn(), getRecipeById: vi.fn() },
}));

describe('NotFoundPage', () => {
  it('explains what happened and offers a way back', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Back to home' })).toBeTruthy();
  });
});

// The test above proves the page renders correctly in isolation, but not that the app's router
// actually reaches it (the real fix for BUG-06). App.tsx mounts its own BrowserRouter, which
// reads from the History API rather than accepting an injected route, so an unknown path is
// simulated the same way BrowserRouter itself would see one: pushing it onto jsdom's history
// before mount.
describe('App routing', () => {
  it('renders the 404 page inside the app shell for an unknown path', () => {
    window.history.pushState({}, '', '/nope');

    render(<App />);

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Back to home' })).toBeTruthy();
    // The shell is still present around the 404 content.
    expect(screen.getByRole('banner')).toBeTruthy();
    expect(screen.getByRole('contentinfo')).toBeTruthy();
  });
});

describe('App routing to a recipe', () => {
  it('renders the detail screen inside the app shell for /recipes/:id', async () => {
    const id = '22222222-2222-2222-2222-222222222222';
    const recipe: Recipe = {
      id, title: 'Cacio e Pepe', description: 'Three ingredients.', preparationTime: 5, cookingTime: 12,
      servings: 2, ingredients: [], instructions: [],
    };
    vi.mocked(recipeService.getRecipeById).mockResolvedValue({ data: recipe } as AxiosResponse<Recipe>);
    window.history.pushState({}, '', `/recipes/${id}`);

    render(
      <QueryClientProvider client={new QueryClient()}>
        <App />
      </QueryClientProvider>,
    );

    expect(await screen.findByRole('heading', { level: 1, name: 'Cacio e Pepe' })).toBeTruthy();
    // The footer, not the banner: dom-testing-library also counts the recipe's own <header> as a banner,
    // although browsers do not give that role to a <header> inside <article>.
    expect(screen.getByRole('contentinfo')).toBeTruthy();
  });
});
