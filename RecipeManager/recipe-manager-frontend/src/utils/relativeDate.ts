// src/utils/relativeDate.ts
// "Thursday", "2 weeks ago": how the editorial design dates a cook (spec 016 §8). Fixed English locale, like the
// rest of the UI copy; native Intl, no date library.

const DAY_MS = 86_400_000;

// Calendar days in the reader's timezone, not 24-hour periods: 23:30 yesterday is "yesterday" forty minutes later.
// Rounded because a day containing a daylight-saving change is 23 or 25 hours long.
export const calendarDaysBetween = (earlier: Date, later: Date): number => {
  const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
  return Math.round((startOfDay(later) - startOfDay(earlier)) / DAY_MS);
};

const weekday = new Intl.DateTimeFormat('en', { weekday: 'long' });
const relative = new Intl.RelativeTimeFormat('en', { numeric: 'always' });
// Assembled rather than en-GB's own format: recent ICU writes September as "Sept" there, so the output would depend
// on the Node or browser version. The plain 'en' short month is "Sep" everywhere.
const shortMonth = new Intl.DateTimeFormat('en', { month: 'short' });
const absolute = (date: Date) => `${date.getDate()} ${shortMonth.format(date)} ${date.getFullYear()}`;

export const formatRelativeDate = (instant: Date, now: Date): string => {
  const days = calendarDaysBetween(instant, now);
  // Negative when the server's clock runs slightly ahead of the browser's: still today, never "in 0 days".
  if (days <= 0) return 'today';
  if (days === 1) return 'yesterday';
  // Within the last week a weekday is unambiguous; from day 7 "Sunday" could mean today's or last week's.
  if (days < 7) return weekday.format(instant);
  // Rounded down: a cook 13 days ago is "1 week ago", never inflated to 2.
  if (days < 30) return relative.format(-Math.floor(days / 7), 'week');
  return absolute(instant);
};
