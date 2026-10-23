// src/pages/Home/HomePage.test.tsx

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { HomePage } from './HomePage';

const lists = vi.hoisted(() => ({
  Published: [
    { id: '1', title: 'Cacio e Pepe', cookCount: 11, lastCookedAt: '2026-10-08T19:00:00Z' },
    { id: '2', title: 'Black Bean Soup', cookCount: 0, lastCookedAt: null },
  ],
  Draft: [{ id: '3', title: 'Focaccia', cookCount: 2, lastCookedAt: '2026-10-10T19:00:00Z' }],
}));

vi.mock('@/hooks/useRecipes', () => ({
  useRecipes: (status: 'Published' | 'Draft' = 'Published') =>
    ({ data: lists[status], isLoading: false, error: null }),
}));

const renderHome = () =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );

describe('HomePage', () => {
  it('shows the live recipe count and every cook in the masthead', () => {
    renderHome();

    expect(screen.getByText('2 recipes · 13 cooks · yours alone')).toBeTruthy();
  });

  it('lists the most recently cooked recipes, drafts included, newest first', () => {
    renderHome();

    const rail = screen.getByRole('complementary', { name: 'Last cooked' });
    const links = within(rail).getAllByRole('link').map(link => link.textContent);
    expect(links).toEqual(['Focaccia', 'Cacio e Pepe']);
    expect(within(rail).getByRole('link', { name: 'Focaccia' }).getAttribute('href')).toBe('/recipes/3');
  });

  it('offers both entry points', () => {
    renderHome();

    expect(screen.getByRole('link', { name: 'Browse recipes' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Add a recipe' })).toBeTruthy();
  });
});

describe('HomePage while the recipe count is unresolved', () => {
  it('still renders the heading and entry points, but not the count', async () => {
    vi.resetModules();
    vi.doMock('@/hooks/useRecipes', () => ({
      useRecipes: () => ({ data: undefined, isLoading: true, error: null }),
    }));
    const { HomePage: LoadingHomePage } = await import('./HomePage');

    render(
      <QueryClientProvider client={new QueryClient()}>
        <MemoryRouter>
          <LoadingHomePage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('Everything you actually cook, in one place.')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Browse recipes' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Add a recipe' })).toBeTruthy();
    expect(screen.queryByText(/recipes · yours alone/)).toBeNull();
  });
});

describe('HomePage with nothing cooked yet', () => {
  it('drops the cook count and the rail', async () => {
    vi.resetModules();
    vi.doMock('@/hooks/useRecipes', () => ({
      useRecipes: () => ({ data: [{ id: '1', title: 'A', cookCount: 0, lastCookedAt: null }], isLoading: false, error: null }),
    }));
    const { HomePage: FreshHomePage } = await import('./HomePage');

    render(
      <QueryClientProvider client={new QueryClient()}>
        <MemoryRouter>
          <FreshHomePage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('1 recipe · yours alone')).toBeTruthy();
    expect(screen.queryByRole('complementary', { name: 'Last cooked' })).toBeNull();
  });
});

describe('HomePage recipe count pluralization', () => {
  // A count fixed at two recipes cannot tell a live count from a hard-coded string, and cannot
  // exercise the singular/plural branch at all. One recipe and zero recipes together force both:
  // a literal '2 recipes · yours alone' fails the singular case, and a hard-coded plural fails
  // the zero case too.
  it('uses the singular "recipe" when there is exactly one', async () => {
    vi.resetModules();
    vi.doMock('@/hooks/useRecipes', () => ({
      useRecipes: () => ({ data: [{ id: '1' }], isLoading: false, error: null }),
    }));
    const { HomePage: SingleRecipeHomePage } = await import('./HomePage');

    render(
      <QueryClientProvider client={new QueryClient()}>
        <MemoryRouter>
          <SingleRecipeHomePage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('1 recipe · yours alone')).toBeTruthy();
  });

  // Every new install starts here. The copy is the brief's, not reworded here even though "0
  // recipes" reads a little oddly — that call belongs to whoever owns the design.
  it('uses the plural "recipes" when there are none yet', async () => {
    vi.resetModules();
    vi.doMock('@/hooks/useRecipes', () => ({
      useRecipes: () => ({ data: [], isLoading: false, error: null }),
    }));
    const { HomePage: EmptyHomePage } = await import('./HomePage');

    render(
      <QueryClientProvider client={new QueryClient()}>
        <MemoryRouter>
          <EmptyHomePage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('0 recipes · yours alone')).toBeTruthy();
  });
});
