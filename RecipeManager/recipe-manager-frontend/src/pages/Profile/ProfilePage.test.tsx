import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ThemeProvider, UnitsProvider } from '@/contexts';
import { ProfilePage } from './ProfilePage';

type Row = { id: string; title: string; cookCount: number; lastCookedAt: string | null };
const lists = vi.hoisted(() => ({ Published: [] as Row[], Draft: [] as Row[] }));

vi.mock('@/hooks/useRecipes', () => ({
  useRecipes: (status: 'Published' | 'Draft' = 'Published') =>
    ({ data: lists[status], isLoading: false, error: null }),
}));

const renderPage = () =>
  render(
    <ThemeProvider>
      <UnitsProvider>
        <MemoryRouter>
          <ProfilePage />
        </MemoryRouter>
      </UnitsProvider>
    </ThemeProvider>,
  );

beforeEach(() => {
  localStorage.clear();
  lists.Published = [];
  lists.Draft = [];
});

describe('ProfilePage stats', () => {
  it('says none yet with no cooks', () => {
    renderPage();

    expect(screen.getByText('none yet')).toBeTruthy();
    expect(screen.getByText('—')).toBeTruthy();
  });

  it('totals every cook and links the most cooked recipe', () => {
    lists.Published = [{ id: '1', title: 'Cacio e Pepe', cookCount: 11, lastCookedAt: '2026-10-08T19:00:00Z' }];
    lists.Draft = [{ id: '3', title: 'Focaccia', cookCount: 2, lastCookedAt: '2026-10-10T19:00:00Z' }];
    renderPage();

    expect(screen.getByText('13')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Cacio e Pepe' }).getAttribute('href')).toBe('/recipes/1');
    expect(screen.getByText('11 times')).toBeTruthy();
  });
});

describe('ProfilePage preferences', () => {
  it('switches the theme', () => {
    renderPage();
    expect(screen.getByRole('radiogroup', { name: 'Theme' })).toBeTruthy();
    // A native radio exposes its state as the DOM `checked` property, not `aria-checked`.
    expect(screen.getByRole<HTMLInputElement>('radio', { name: 'System' }).checked).toBe(true);

    fireEvent.click(screen.getByRole('radio', { name: 'Dark' }));

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('offers As written, Metric and Imperial, defaulting to As written', () => {
    renderPage();
    expect(screen.getByRole('radiogroup', { name: 'Units' })).toBeTruthy();
    expect(screen.getByText('How quantities are shown')).toBeTruthy();
    expect(screen.getByRole<HTMLInputElement>('radio', { name: 'As written' }).checked).toBe(true);

    fireEvent.click(screen.getByRole('radio', { name: 'Imperial' }));

    expect(screen.getByRole<HTMLInputElement>('radio', { name: 'Imperial' }).checked).toBe(true);
    expect(localStorage.getItem('units')).toBe('imperial');
  });
});
