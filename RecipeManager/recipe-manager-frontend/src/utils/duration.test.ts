import { describe, expect, it } from 'vitest';
import { formatDuration, getISODuration } from '@/utils/duration';

describe('formatDuration', () => {
  it.each<[number, string]>([
    [0, '0 min'],
    [59, '59 min'],
    [60, '1h'],
    [61, '1h 1min'],
    [120, '2h'],
    [NaN, '0 min'],
    [Infinity, '0 min'],
    [-5, '0 min'],
  ])('%s minutes → "%s"', (minutes, expected) => {
    expect(formatDuration(minutes)).toBe(expected);
  });
});

describe('getISODuration', () => {
  it.each<[number, string]>([
    [0, 'PT0M'],
    [59, 'PT59M'],
    [60, 'PT1H'],
    [61, 'PT1H1M'],
    [120, 'PT2H'],
    [NaN, 'PT0M'],
    [Infinity, 'PT0M'],
    [-5, 'PT0M'],
  ])('%s minutes → "%s"', (minutes, expected) => {
    expect(getISODuration(minutes)).toBe(expected);
  });
});
