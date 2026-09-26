import { act, renderHook } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { useMediaQuery } from './useMediaQuery';

const original = window.matchMedia;

// A controllable MediaQueryList: flip `matches` and fire `change`, as a real resize would.
const installMatchMedia = (initial: boolean) => {
  let matches = initial;
  const listeners = new Set<() => void>();
  window.matchMedia = (query: string) =>
    ({
      get matches() { return matches; },
      media: query,
      addEventListener: (_: string, listener: () => void) => listeners.add(listener),
      removeEventListener: (_: string, listener: () => void) => listeners.delete(listener),
    }) as unknown as MediaQueryList;
  return {
    set: (next: boolean) => { matches = next; listeners.forEach(listener => listener()); },
    listenerCount: () => listeners.size,
  };
};

afterEach(() => {
  window.matchMedia = original;
});

describe('useMediaQuery', () => {
  it('returns the current match', () => {
    installMatchMedia(true);
    const { result } = renderHook(() => useMediaQuery('(max-width: 768px)'));
    expect(result.current).toBe(true);
  });

  it('re-renders when the media query changes', () => {
    const media = installMatchMedia(false);
    const { result } = renderHook(() => useMediaQuery('(max-width: 768px)'));

    act(() => media.set(true));

    expect(result.current).toBe(true);
  });

  it('unsubscribes on unmount', () => {
    const media = installMatchMedia(false);
    const { unmount } = renderHook(() => useMediaQuery('(max-width: 768px)'));

    unmount();

    expect(media.listenerCount()).toBe(0);
  });
});
