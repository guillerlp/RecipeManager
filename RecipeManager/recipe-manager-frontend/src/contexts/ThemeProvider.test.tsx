import { fireEvent, render, renderHook, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useTheme } from '@/hooks/useTheme';
import { ThemeProvider } from './ThemeProvider';

function ThemeProbe() {
  const { theme, toggleTheme } = useTheme();
  return <button onClick={toggleTheme}>{theme}</button>;
}

const renderWithProvider = () =>
  render(
    <ThemeProvider>
      <ThemeProbe />
    </ThemeProvider>,
  );

const dataTheme = () => document.documentElement.getAttribute('data-theme');

beforeEach(() => {
  localStorage.clear();
  document.documentElement.removeAttribute('data-theme');
});

describe('ThemeProvider', () => {
  it.each<{ saved: string | null; expected: string }>([
    { saved: 'dark', expected: 'dark' },
    { saved: 'light', expected: 'light' },
    { saved: null, expected: 'light' },
    { saved: 'purple', expected: 'light' },
  ])('starts as $expected when localStorage holds $saved', ({ saved, expected }) => {
    if (saved !== null) localStorage.setItem('theme', saved);

    renderWithProvider();

    expect(screen.getByRole('button').textContent).toBe(expected);
    expect(dataTheme()).toBe(expected);
  });

  it('toggleTheme flips the theme, the data-theme attribute, and the stored value', () => {
    renderWithProvider();

    fireEvent.click(screen.getByRole('button'));

    expect(screen.getByRole('button').textContent).toBe('dark');
    expect(dataTheme()).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });
});

describe('useTheme', () => {
  it('throws outside a ThemeProvider', () => {
    // React logs the render error before rethrowing it; silence that so the expected throw is not noise.
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    expect(() => renderHook(() => useTheme())).toThrow('useTheme must be used within a ThemeProvider');

    consoleError.mockRestore();
  });
});
