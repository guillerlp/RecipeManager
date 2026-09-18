import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { NavLink } from './NavLink';

// aria-current is the assertion, not the CSS class: it is what assistive technology reads,
// and it is driven by the same isActive flag as the class.
const ariaCurrentFor = (to: string, path: string) => {
  render(
    <MemoryRouter initialEntries={[path]}>
      <NavLink to={to}>Recipes</NavLink>
    </MemoryRouter>,
  );
  return screen.getByRole('link').getAttribute('aria-current');
};

describe('NavLink', () => {
  it.each<{ to: string; path: string; expected: string | null }>([
    { to: '/recipes', path: '/recipes', expected: 'page' },
    { to: '/recipes', path: '/recipes/', expected: 'page' },
    { to: '/recipes/', path: '/recipes', expected: 'page' },
    { to: '/recipes', path: '/recipes/123', expected: 'page' },
    { to: '/recipes', path: '/recipes-archive', expected: null },
    { to: '/', path: '/', expected: 'page' },
    { to: '/', path: '/recipes', expected: null },
  ])('$to on $path → aria-current $expected', ({ to, path, expected }) => {
    expect(ariaCurrentFor(to, path)).toBe(expected);
  });
});
