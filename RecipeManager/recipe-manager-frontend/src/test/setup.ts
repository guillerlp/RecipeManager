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

// jsdom implements <dialog> without showModal/close. This stands in for what the tests rely on: the open attribute,
// and the close event a real browser fires (on Esc too). Focus trapping and the inert background are not emulated —
// they are checked by hand (spec 015 §12).
if (typeof HTMLDialogElement !== 'undefined' && typeof HTMLDialogElement.prototype.showModal !== 'function') {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.setAttribute('open', '');
  };
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.removeAttribute('open');
    this.dispatchEvent(new Event('close'));
  };
}
