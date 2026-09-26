import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { SegmentedControl } from './SegmentedControl';

const OPTIONS = [
  { value: 'a', label: 'Alpha' },
  { value: 'b', label: 'Beta' },
] as const;

describe('SegmentedControl', () => {
  it('is one radio group named by its legend, with the value checked', () => {
    render(<SegmentedControl legend="Letters" name="letters" options={OPTIONS} value="b" onChange={() => undefined} />);

    expect(screen.getByRole('radiogroup', { name: 'Letters' })).toBeTruthy();
    expect(screen.getAllByRole('radio')).toHaveLength(2);
    expect(screen.getByRole<HTMLInputElement>('radio', { name: 'Beta' }).checked).toBe(true);
  });

  it('reports the chosen option', () => {
    const onChange = vi.fn();
    render(<SegmentedControl legend="Letters" name="letters" options={OPTIONS} value="b" onChange={onChange} />);

    fireEvent.click(screen.getByRole('radio', { name: 'Alpha' }));

    expect(onChange).toHaveBeenCalledWith('a');
  });
});
