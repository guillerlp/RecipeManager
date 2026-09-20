// src/pages/Home/HomePage.test.tsx

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { HomePage } from './HomePage';

vi.mock('@/hooks/useRecipes', () => ({
  useRecipes: () => ({ data: [{ id: '1' }, { id: '2' }], isLoading: false, error: null }),
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
  it('shows the live recipe count in the masthead', () => {
    renderHome();

    expect(screen.getByText('2 recipes · yours alone')).toBeTruthy();
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
