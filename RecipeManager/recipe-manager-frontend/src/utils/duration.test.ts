import { describe, expect, it } from 'vitest';
import { formatDuration, getISODuration } from '@/utils/duration';

describe('formatDuration', () => {
  it.each<{ minutes: number; expected: string }>([
    { minutes: 0, expected: '1 min' },
    { minutes: 59, expected: '59 min' },
    { minutes: 60, expected: '1h' },
    { minutes: 61, expected: '1h 1min' },
    { minutes: 120, expected: '2h' },
    { minutes: NaN, expected: '0 min' },
    { minutes: Infinity, expected: '0 min' },
    { minutes: -5, expected: '0 min' },
  ])('$minutes minutes → "$expected"', ({ minutes, expected }) => {
    expect(formatDuration(minutes)).toBe(expected);
  });
});

describe('getISODuration', () => {
  it.each<{ minutes: number; expected: string }>([
    { minutes: 0, expected: 'PT0M' },
    { minutes: 59, expected: 'PT59M' },
    { minutes: 60, expected: 'PT1H' },
    { minutes: 61, expected: 'PT1H1M' },
    { minutes: 120, expected: 'PT2H' },
    { minutes: NaN, expected: 'PT0M' },
    { minutes: Infinity, expected: 'PT0M' },
    { minutes: -5, expected: 'PT0M' },
  ])('$minutes minutes → "$expected"', ({ minutes, expected }) => {
    expect(getISODuration(minutes)).toBe(expected);
  });
});
