import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { BottomNav } from './BottomNav';

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <BottomNav />
    </MemoryRouter>,
  );

describe('BottomNav', () => {
  it('marks only the current destination', () => {
    renderAt('/recipes');

    expect(screen.getByRole('link', { name: 'Recipes' }).getAttribute('aria-current')).toBe('page');
    expect(screen.getByRole('link', { name: 'Home' }).getAttribute('aria-current')).toBeNull();
  });

  it('labels itself so a screen reader can tell it from the header nav', () => {
    renderAt('/');

    expect(screen.getByRole('navigation', { name: 'Primary navigation' })).toBeTruthy();
  });

  it('marks only "Add" as current on /recipes/new, not "Recipes" too', () => {
    renderAt('/recipes/new');

    const current = screen.getAllByRole('link').filter((link) => link.getAttribute('aria-current') === 'page');
    expect(current).toHaveLength(1);
    expect(current[0].textContent).toContain('Add');
  });

  it('marks "Recipes" current on a nested recipe path', () => {
    renderAt('/recipes/123');

    expect(screen.getByRole('link', { name: 'Recipes' }).getAttribute('aria-current')).toBe('page');
  });
});
