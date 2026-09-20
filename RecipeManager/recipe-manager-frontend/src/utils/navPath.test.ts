import { describe, expect, it } from 'vitest';
import { isPathActive } from '@/utils/navPath';

describe('isPathActive', () => {
  it.each<{ target: string; pathname: string; expected: boolean }>([
    { target: '/recipes', pathname: '/recipes', expected: true },
    { target: '/recipes', pathname: '/recipes/', expected: true },
    { target: '/recipes/', pathname: '/recipes', expected: true },
    { target: '/recipes', pathname: '/recipes/123', expected: true },
    { target: '/recipes', pathname: '/recipes-archive', expected: false },
    { target: '/', pathname: '/', expected: true },
    { target: '/', pathname: '/recipes', expected: false },
  ])('isPathActive($target, $pathname) → $expected', ({ target, pathname, expected }) => {
    expect(isPathActive(target, pathname)).toBe(expected);
  });
});
