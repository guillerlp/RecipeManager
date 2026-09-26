import { useCallback, useSyncExternalStore } from 'react';

// useSyncExternalStore rather than useState + useEffect: the value is read during render, so the
// first paint is already correct (no flash of the desktop layout on a phone) and React cannot
// render with a stale value between a change event and the effect that would have copied it.
export const useMediaQuery = (query: string): boolean => {
  const subscribe = useCallback(
    (onChange: () => void) => {
      const list = window.matchMedia(query);
      list.addEventListener('change', onChange);
      return () => list.removeEventListener('change', onChange);
    },
    [query],
  );

  return useSyncExternalStore(subscribe, () => window.matchMedia(query).matches);
};
