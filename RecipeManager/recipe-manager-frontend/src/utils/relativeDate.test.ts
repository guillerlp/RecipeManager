import { describe, expect, it } from 'vitest';
import { calendarDaysBetween, formatRelativeDate } from '@/utils/relativeDate';

// Local times throughout: "today" and "yesterday" are the reader's calendar days. 2026-10-11 is a Sunday.
const now = new Date(2026, 9, 11, 20, 0);

describe('formatRelativeDate', () => {
  it.each<{ instant: Date; expected: string }>([
    { instant: new Date(2026, 9, 11, 8, 0), expected: 'today' },
    { instant: new Date(2026, 9, 10, 23, 30), expected: 'yesterday' },
    { instant: new Date(2026, 9, 8, 19, 0), expected: 'Thursday' },
    { instant: new Date(2026, 9, 5, 12, 0), expected: 'Monday' },
    { instant: new Date(2026, 9, 4, 12, 0), expected: '1 week ago' },
    { instant: new Date(2026, 8, 27, 12, 0), expected: '2 weeks ago' },
    { instant: new Date(2026, 8, 12, 12, 0), expected: '4 weeks ago' },
    { instant: new Date(2026, 8, 11, 12, 0), expected: '11 Sep 2026' },
    { instant: new Date(2025, 2, 3, 12, 0), expected: '3 Mar 2025' },
  ])('$instant → "$expected"', ({ instant, expected }) => {
    expect(formatRelativeDate(instant, now)).toBe(expected);
  });

  it('reads a cook just after midnight as today until the day ends', () => {
    expect(formatRelativeDate(new Date(2026, 9, 11, 0, 5), new Date(2026, 9, 11, 23, 55))).toBe('today');
  });

  it('reads a cook just before midnight as yesterday a few minutes later', () => {
    expect(formatRelativeDate(new Date(2026, 9, 10, 23, 55), new Date(2026, 9, 11, 0, 5))).toBe('yesterday');
  });

  it('reads an instant slightly in the future (server clock ahead) as today', () => {
    expect(formatRelativeDate(new Date(2026, 9, 11, 20, 0, 30), now)).toBe('today');
  });
});

describe('calendarDaysBetween', () => {
  it('counts calendar days, not 24-hour periods', () => {
    expect(calendarDaysBetween(new Date(2026, 9, 10, 23, 59), new Date(2026, 9, 11, 0, 1))).toBe(1);
    expect(calendarDaysBetween(new Date(2026, 9, 11, 0, 1), new Date(2026, 9, 11, 23, 59))).toBe(0);
  });

  it('is exact across a daylight-saving change', () => {
    // Most of Europe leaves summer time on 2026-10-25, so that day is 25 hours long where the tests run.
    expect(calendarDaysBetween(new Date(2026, 9, 24, 12), new Date(2026, 9, 26, 12))).toBe(2);
  });
});
