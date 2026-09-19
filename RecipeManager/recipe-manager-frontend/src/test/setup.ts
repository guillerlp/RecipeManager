import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// RTL unmounts rendered trees automatically only when Vitest globals are on. They are off
// (ADR-018), so without this every test would leak its DOM into the next.
afterEach(() => {
  cleanup();
});

// jsdom implements no matchMedia, and ThemeProvider calls it on first render (ADR-018 setup file).
// Defaults to "OS prefers light"; a test wanting dark overrides window.matchMedia itself.
if (!window.matchMedia) {
  window.matchMedia = (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  });
}
