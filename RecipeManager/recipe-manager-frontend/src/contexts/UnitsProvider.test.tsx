import { act, render, renderHook, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useUnits } from '@/hooks';
import { UnitsProvider } from './UnitsProvider';

const Probe = () => <p>{useUnits().unitSystem}</p>;
const renderProbe = () => render(<UnitsProvider><Probe /></UnitsProvider>);

beforeEach(() => localStorage.clear());
afterEach(() => vi.restoreAllMocks());

describe('UnitsProvider', () => {
  it('defaults to As written', () => {
    renderProbe();
    expect(screen.getByText('asWritten')).toBeTruthy();
  });

  it('restores a stored preference and ignores an unrecognised one', () => {
    localStorage.setItem('units', 'imperial');
    const { unmount } = renderProbe();
    expect(screen.getByText('imperial')).toBeTruthy();
    unmount();

    localStorage.setItem('units', 'kelvin');
    renderProbe();
    expect(screen.getByText('asWritten')).toBeTruthy();
  });

  it('persists a change', () => {
    const { result } = renderHook(() => useUnits(), { wrapper: UnitsProvider });
    act(() => result.current.setUnitSystem('metric'));
    expect(result.current.unitSystem).toBe('metric');
    expect(localStorage.getItem('units')).toBe('metric');
  });

  it('falls back to As written when storage throws', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('denied'); });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('denied'); });
    renderProbe();
    expect(screen.getByText('asWritten')).toBeTruthy();
  });

  it('refuses to run outside its provider', () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    expect(() => renderHook(() => useUnits())).toThrow('useUnits must be used within a UnitsProvider');
  });
});
