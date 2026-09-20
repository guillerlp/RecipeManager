// Triple-slash reference widens the TypeScript program's ambient types to include Node globals
// (process, __dirname, Buffer) visible to all src/ files, not just this file. Required because
// `node:fs` and `node:url` imports do not resolve without it despite @types/node being installed.
/// <reference types="node" />

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

// An asymmetric token resolves to nothing in one theme and fails silently, which is why this is a
// test rather than a review checklist item (docs/agents/07-ux-ui.md).
const read = (file: string): string =>
  readFileSync(fileURLToPath(new URL(file, import.meta.url)), 'utf8');

const tokensOf = (css: string): Set<string> =>
  new Set([...css.matchAll(/(--[a-z0-9-]+)\s*:/g)].map((m) => m[1]));

const EXPECTED = [
  '--paper', '--paper-2', '--ink', '--ink-2', '--ink-3',
  '--rule', '--accent', '--accent-text', '--danger', '--field-border',
];

describe('theme token parity', () => {
  const light = tokensOf(read('./light.css'));
  const dark = tokensOf(read('./dark.css'));

  it('declares the same tokens in both themes', () => {
    expect([...light].sort()).toEqual([...dark].sort());
  });

  it.each(EXPECTED)('declares %s', (token) => {
    expect(light.has(token)).toBe(true);
    expect(dark.has(token)).toBe(true);
  });

  it('no longer declares the superseded --color-* tokens', () => {
    expect([...light].filter((t) => t.startsWith('--color-'))).toEqual([]);
    expect([...dark].filter((t) => t.startsWith('--color-'))).toEqual([]);
  });
});
