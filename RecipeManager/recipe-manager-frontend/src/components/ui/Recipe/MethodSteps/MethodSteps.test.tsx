import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { InstructionStep } from '@/types';
import { MethodSteps } from './MethodSteps';

const step = (id: string, text: string): InstructionStep => ({ id, text, durationMinutes: null, ingredientIds: [] });

describe('MethodSteps', () => {
  it('renders the steps in order as a list, with role="list" so VoiceOver keeps list semantics', () => {
    render(<MethodSteps steps={[step('a', 'Heat the oven.'), step('b', 'Roast.')]} />);

    const list = screen.getByRole('list');
    expect(list.tagName).toBe('OL');
    expect(list.getAttribute('role')).toBe('list');
    expect(screen.getAllByRole('listitem').map(item => item.textContent)).toEqual(['1Heat the oven.', '2Roast.']);
  });
});
