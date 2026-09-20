// src/components/ui/ThemeControl/ThemeControl.test.tsx

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ThemeProvider } from '@/contexts';
import { ThemeControl } from './ThemeControl';

const renderControl = () =>
  render(
    <ThemeProvider>
      <ThemeControl />
    </ThemeProvider>,
  );

describe('ThemeControl', () => {
  it('offers the three preferences as one radio group', () => {
    renderControl();

    const group = screen.getByRole('radiogroup', { name: 'Theme' });
    expect(group).toBeTruthy();
    expect(screen.getAllByRole('radio')).toHaveLength(3);
  });

  it('marks the current preference and changes it on selection', () => {
    localStorage.clear();
    renderControl();

    // A native radio exposes its state as the DOM `checked` property, not an `aria-checked`
    // attribute (that belongs to the ARIA widget pattern this control deliberately does not
    // use — see ThemeControl.tsx).
    expect(screen.getByRole<HTMLInputElement>('radio', { name: 'System' }).checked).toBe(true);

    fireEvent.click(screen.getByRole('radio', { name: 'Dark' }));

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });
});
