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

  it('cannot go below 1', () => {
    render(<ServingsStepper value={1} onChange={() => undefined} />);
    expect(screen.getByRole<HTMLButtonElement>('button', { name: 'Decrease servings' }).disabled).toBe(true);
  });

  it('cannot go above 999, the servings validator bound', () => {
    render(<ServingsStepper value={999} onChange={() => undefined} />);
    expect(screen.getByRole<HTMLButtonElement>('button', { name: 'Increase servings' }).disabled).toBe(true);
  });
});
