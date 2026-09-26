import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ServingsStepper } from './ServingsStepper';

describe('ServingsStepper', () => {
  it('is a labelled group showing the value', () => {
    render(<ServingsStepper value={4} onChange={() => undefined} />);

    expect(screen.getByRole('group', { name: 'Servings' })).toBeTruthy();
    expect(screen.getByRole('status').textContent).toBe('4');
  });

  it('steps down and up by one', () => {
    const onChange = vi.fn();
    render(<ServingsStepper value={4} onChange={onChange} />);

    fireEvent.click(screen.getByRole('button', { name: 'Decrease servings' }));
    fireEvent.click(screen.getByRole('button', { name: 'Increase servings' }));

    expect(onChange.mock.calls).toEqual([[3], [5]]);
  });

  // aria-disabled, not the disabled attribute: disabling the button that has focus (pressing − down to 1)
  // would drop keyboard focus to <body> and a screen-reader user would lose their place.
  it.each<[string, number]>([
    ['Decrease servings', 1],
    ['Increase servings', 999],
  ])('%s is unavailable at %i but keeps focus', (name, value) => {
    const onChange = vi.fn();
    render(<ServingsStepper value={value} onChange={onChange} />);
    const button = screen.getByRole<HTMLButtonElement>('button', { name });
    button.focus();

    fireEvent.click(button);

    expect(button.getAttribute('aria-disabled')).toBe('true');
    expect(button.disabled).toBe(false);
    expect(document.activeElement).toBe(button);
    expect(onChange).not.toHaveBeenCalled();
  });
});
