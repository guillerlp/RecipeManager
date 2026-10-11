// src/pages/RecipeDetail/CookControls.tsx

import { useState } from 'react';
import { useDeleteCook, useLogCook } from '@/hooks';
import type { Cook } from '@/types';
import styles from './RecipeDetailPage.module.css';

const pad = (n: number) => String(n).padStart(2, '0');

// The reader's calendar date as <input type="date"> expects it (YYYY-MM-DD), not toISOString()'s UTC date.
const localIsoDate = (date: Date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

// A picked day becomes local noon, sent as UTC: midnight would land on the previous UTC day for anyone east of
// Greenwich. Today sends nothing — noon may still be ahead, and the server rejects a future cook.
const cookedAtFor = (day: string): string | undefined => {
  if (day === localIsoDate(new Date())) return undefined;
  const [year, month, date] = day.split('-').map(Number);
  return new Date(year, month - 1, date, 12).toISOString();
};

export const CookControls = ({ recipeId }: { recipeId: string }) => {
  const logCook = useLogCook();
  const deleteCook = useDeleteCook();
  // Every cook logged on this visit, newest last, so Undo can name each one: with no cook list (UX-20), a cook that
  // lost its Undo could never be removed. Not timed: Undo lasts until the page is left (WCAG 2.2.1), because the API
  // puts no deadline on a delete either.
  const [loggedThisVisit, setLoggedThisVisit] = useState<Cook[]>([]);
  const logged: Cook | null = loggedThisVisit[loggedThisVisit.length - 1] ?? null;
  const [day, setDay] = useState('');

  const log = (cookedAt?: string) =>
    logCook.mutate({ recipeId, cookedAt }, {
      onSuccess: cook => { setLoggedThisVisit(cooks => [...cooks, cook]); setDay(''); },
    });

  const undo = (cook: Cook) =>
    deleteCook.mutate({ recipeId, cookId: cook.id }, {
      onSuccess: () => setLoggedThisVisit(cooks => cooks.filter(c => c.id !== cook.id)),
    });

  return (
    <div className={styles.cook}>
      <div className={styles.cookActions}>
        {logged ? (
          <button type="button" className={styles.cookButton} onClick={() => undo(logged)}
            disabled={deleteCook.isPending}>
            Undo
          </button>
        ) : (
          <button type="button" className={styles.cookButton} onClick={() => log()} disabled={logCook.isPending}>
            I cooked this
          </button>
        )}
        <p role="status" className={styles.cookStatus}>{logged ? 'Logged' : ''}</p>
      </div>

      <details>
        <summary className={styles.backdateSummary}>Cooked on another day?</summary>
        <form
          className={styles.backdateForm}
          onSubmit={event => {
            event.preventDefault();
            log(cookedAtFor(day));
          }}
        >
          <label>
            Day{' '}
            <input type="date" required max={localIsoDate(new Date())} value={day}
              onChange={event => setDay(event.target.value)} />
          </label>
          <button type="submit" className={styles.cookButton} disabled={logCook.isPending}>Log</button>
        </form>
      </details>

      {/* The error's own message is deliberately not rendered: it can carry server detail (SEC-05). */}
      {logCook.isError && <p role="alert" className={styles.cookError}>Couldn't log this cook. Try again.</p>}
      {deleteCook.isError && <p role="alert" className={styles.cookError}>Couldn't undo that. Try again.</p>}
    </div>
  );
};
