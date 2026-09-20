import { act, fireEvent, render, renderHook, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useTheme } from '@/hooks/useTheme';
import { ThemeProvider } from './ThemeProvider';

type MediaListener = (event: MediaQueryListEvent) => void;

/** Replaces window.matchMedia with one that reports `prefersDark` and can fire a change. */
function mockMatchMedia(prefersDark: boolean) {
  const listeners: MediaListener[] = [];
  window.matchMedia = ((query: string) => ({
    matches: prefersDark,
    media: query,
    onchange: null,
    addEventListener: (_: string, l: MediaListener) => listeners.push(l),
    removeEventListener: (_: string, l: MediaListener) => {
      const i = listeners.indexOf(l);
      if (i >= 0) listeners.splice(i, 1);
    },
    addListener: (l: MediaListener) => listeners.push(l),
    removeListener: () => undefined,
    dispatchEvent: () => false,
  })) as unknown as typeof window.matchMedia;
  return {
    change: (matches: boolean) =>
      listeners.forEach((l) => l({ matches } as MediaQueryListEvent)),
  };
}

function ThemeProbe() {
  const { theme, preference, setPreference, toggleTheme } = useTheme();
  return (
    <>
      <span data-testid="preference">{preference}</span>
      <button onClick={toggleTheme}>{theme}</button>
      {/* Labelled distinctly from the toggle button above, whose own label IS the theme value
          ('light'/'dark') — a name of 'dark' would collide with it whenever theme is dark. */}
      <button onClick={() => setPreference('dark')}>set dark</button>
    </>
  );
}

const renderWithProvider = () =>
  render(
    <ThemeProvider>
      <ThemeProbe />
    </ThemeProvider>,
  );

const dataTheme = () => document.documentElement.getAttribute('data-theme');

// mockMatchMedia overwrites window.matchMedia globally with no built-in restore; without this,
// correctness depends on describes running in declaration order (so later ones inherit an
// earlier test's mock), which breaks under sequence.shuffle or any reorder.
const originalMatchMedia = window.matchMedia;

beforeEach(() => {
  localStorage.clear();
  document.documentElement.removeAttribute('data-theme');
});

afterEach(() => {
  window.matchMedia = originalMatchMedia;
});

describe('ThemeProvider', () => {
  it.each<{ saved: string | null; expected: string }>([
    { saved: 'dark', expected: 'dark' },
    { saved: 'light', expected: 'light' },
    { saved: 'purple', expected: 'light' },
  ])('starts as $expected when localStorage holds $saved', ({ saved, expected }) => {
    if (saved !== null) localStorage.setItem('theme', saved);

    renderWithProvider();

    // Positional: the probe has a second, static "dark" button, so a name-based query
    // is ambiguous whenever the toggle button also reads "dark".
    expect(screen.getAllByRole('button')[0].textContent).toBe(expected);
    expect(dataTheme()).toBe(expected);
  });

  // No stored value now means preference 'system', not preference 'light' — the rendered
  // theme is still light (the stubbed OS prefers light), but that is no longer the same claim.
  it('starts as light when nothing is stored in localStorage', () => {
    renderWithProvider();

    expect(dataTheme()).toBe('light');
  });

  it('toggleTheme flips the theme, the data-theme attribute, and the stored value', () => {
    renderWithProvider();

    fireEvent.click(screen.getAllByRole('button')[0]);

    expect(screen.getAllByRole('button')[0].textContent).toBe('dark');
    expect(dataTheme()).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });
});

describe('useTheme', () => {
  it('throws outside a ThemeProvider', () => {
    // React logs the render error before rethrowing it; silence that so the expected throw is not noise.
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    try {
      expect(() => renderHook(() => useTheme())).toThrow('useTheme must be used within a ThemeProvider');
    } finally {
      consoleError.mockRestore();
    }
  });
});

describe('system preference', () => {
  it('defaults to system and follows a dark OS on first paint', () => {
    mockMatchMedia(true);

    renderWithProvider();

    expect(screen.getByTestId('preference').textContent).toBe('system');
    expect(dataTheme()).toBe('dark');
  });

  it('follows the OS while the app is open', () => {
    const media = mockMatchMedia(false);
    renderWithProvider();
    expect(dataTheme()).toBe('light');

    act(() => media.change(true));

    expect(dataTheme()).toBe('dark');
  });

  it('an explicit preference wins over the OS', () => {
    mockMatchMedia(true);
    localStorage.setItem('theme', 'light');

    renderWithProvider();

    expect(dataTheme()).toBe('light');
  });

  it('ignores the OS after the preference moves away from system', () => {
    // OS starts dark, so pinning the preference to 'dark' below is not yet distinguishable from
    // "still following the OS" — the discriminating step is flipping the OS to light afterwards.
    const media = mockMatchMedia(true);
    renderWithProvider();

    fireEvent.click(screen.getByRole('button', { name: 'set dark' }));
    // A real transition (true -> false, not a re-fire of the value already in effect, which
    // React would bail out of without even re-rendering). If the gate were broken and 'dark'
    // merely tracked the OS, this would flip the theme to light; the pinned preference must not.
    act(() => media.change(false));

    expect(dataTheme()).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('falls back to system when the stored value is unrecognised', () => {
    mockMatchMedia(false);
    localStorage.setItem('theme', 'purple');

    renderWithProvider();

    expect(screen.getByTestId('preference').textContent).toBe('system');
  });
});
