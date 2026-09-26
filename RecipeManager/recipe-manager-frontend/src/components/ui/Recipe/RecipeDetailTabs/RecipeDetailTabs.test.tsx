import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { RecipeDetailTabs } from './RecipeDetailTabs';

const renderTabs = () =>
  render(<RecipeDetailTabs ingredients={<p>ingredient panel</p>} method={<p>method panel</p>} />);

const tab = (name: string) => screen.getByRole('tab', { name });

describe('RecipeDetailTabs', () => {
  it('selects Ingredients first and hides the Method panel', () => {
    renderTabs();

    expect(tab('Ingredients').getAttribute('aria-selected')).toBe('true');
    expect(tab('Method').getAttribute('aria-selected')).toBe('false');
    expect(screen.getByRole('tabpanel').textContent).toBe('ingredient panel');
    expect(screen.getByText('method panel').closest('[role="tabpanel"]')?.hasAttribute('hidden')).toBe(true);
  });

  it('links each tab to its panel', () => {
    renderTabs();

    const panel = screen.getByRole('tabpanel');
    expect(tab('Ingredients').getAttribute('aria-controls')).toBe(panel.id);
    expect(panel.getAttribute('aria-labelledby')).toBe(tab('Ingredients').id);
  });

  it('keeps only the selected tab in the tab order (roving tabindex)', () => {
    renderTabs();

    expect(tab('Ingredients').tabIndex).toBe(0);
    expect(tab('Method').tabIndex).toBe(-1);
  });

  it('switches panel on click', () => {
    renderTabs();

    fireEvent.click(tab('Method'));

    expect(screen.getByRole('tabpanel').textContent).toBe('method panel');
  });

  it.each<[string, string]>([
    ['ArrowRight', 'Method'],
    ['ArrowLeft', 'Method'], // wraps from the first tab to the last
    ['End', 'Method'],
  ])('%s from Ingredients selects and focuses %s', (key, expected) => {
    renderTabs();

    fireEvent.keyDown(tab('Ingredients'), { key });

    expect(tab(expected).getAttribute('aria-selected')).toBe('true');
    expect(document.activeElement).toBe(tab(expected));
  });

  it('Home returns to the first tab, and ArrowRight wraps from the last', () => {
    renderTabs();
    fireEvent.click(tab('Method'));

    fireEvent.keyDown(tab('Method'), { key: 'Home' });
    expect(tab('Ingredients').getAttribute('aria-selected')).toBe('true');

    fireEvent.keyDown(tab('Ingredients'), { key: 'End' });
    fireEvent.keyDown(tab('Method'), { key: 'ArrowRight' });
    expect(tab('Ingredients').getAttribute('aria-selected')).toBe('true');
  });
});
