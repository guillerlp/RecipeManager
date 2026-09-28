import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { ThemeProvider, UnitsProvider } from '@/contexts';
import { ProfilePage } from './ProfilePage';

const renderPage = () =>
  render(
    <ThemeProvider>
      <UnitsProvider>
        <ProfilePage />
      </UnitsProvider>
    </ThemeProvider>,
  );

beforeEach(() => localStorage.clear());

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
