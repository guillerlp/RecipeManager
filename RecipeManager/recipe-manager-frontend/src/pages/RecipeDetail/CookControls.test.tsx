import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { AxiosResponse } from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { recipeService } from '@/services';
import type { Cook } from '@/types';
import { CookControls } from './CookControls';

vi.mock('@/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@/services')>()),
  recipeService: { logCook: vi.fn(), deleteCook: vi.fn() },
}));

const logCook = vi.mocked(recipeService.logCook);
const deleteCook = vi.mocked(recipeService.deleteCook);
const ID = '11111111-1111-1111-1111-111111111111';
const cook: Cook = { id: 'c1', cookedAt: '2026-10-11T07:00:00+00:00' };
const ok = <T,>(data: T) => ({ data }) as AxiosResponse<T>;

const renderControls = () =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <CookControls recipeId={ID} />
    </QueryClientProvider>,
  );

beforeEach(() => {
  vi.resetAllMocks();
  // Only Date is faked, so TanStack Query's timers and findBy* still run. 2026-10-11 09:00 local, a Sunday.
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(2026, 9, 11, 9, 0));
  logCook.mockResolvedValue(ok(cook));
  deleteCook.mockResolvedValue(ok(undefined));
});

afterEach(() => vi.useRealTimers());

describe('CookControls', () => {
  it('logs a cook with no date, then offers Undo and announces it', async () => {
    renderControls();

    fireEvent.click(screen.getByRole('button', { name: 'I cooked this' }));

    expect(await screen.findByRole('button', { name: 'Undo' })).toBeTruthy();
    expect(logCook).toHaveBeenCalledWith(ID, undefined);
    expect(screen.getByRole('status').textContent).toBe('Logged');
  });

  it('Undo deletes that cook and brings the button back', async () => {
    renderControls();
    fireEvent.click(screen.getByRole('button', { name: 'I cooked this' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Undo' }));

    expect(await screen.findByRole('button', { name: 'I cooked this' })).toBeTruthy();
    expect(deleteCook).toHaveBeenCalledWith(ID, 'c1');
    expect(screen.getByRole('status').textContent).toBe('');
  });

  it('keeps Undo for every cook logged on this visit, newest first', async () => {
    logCook.mockResolvedValueOnce(ok(cook)).mockResolvedValueOnce(ok({ id: 'c2', cookedAt: '2026-10-08T10:00:00Z' }));
    renderControls();
    fireEvent.click(screen.getByRole('button', { name: 'I cooked this' }));
    await screen.findByRole('button', { name: 'Undo' });

    fireEvent.change(screen.getByLabelText('Day'), { target: { value: '2026-10-08' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log' }));
    await waitFor(() => expect(logCook).toHaveBeenCalledTimes(2));

    fireEvent.click(await screen.findByRole('button', { name: 'Undo' }));
    await waitFor(() => expect(deleteCook).toHaveBeenCalledWith(ID, 'c2'));
    fireEvent.click(await screen.findByRole('button', { name: 'Undo' }));
    await waitFor(() => expect(deleteCook).toHaveBeenLastCalledWith(ID, 'c1'));

    expect(await screen.findByRole('button', { name: 'I cooked this' })).toBeTruthy();
  });

  it('cannot be tapped twice while the first log is in flight', async () => {
    logCook.mockReturnValue(new Promise<AxiosResponse<Cook>>(() => undefined));
    renderControls();
    const button = screen.getByRole<HTMLButtonElement>('button', { name: 'I cooked this' });

    fireEvent.click(button);
    // TanStack Query flushes the pending state on its own schedule, not synchronously inside the click.
    await waitFor(() => expect(button.disabled).toBe(true));
    fireEvent.click(button);

    expect(logCook).toHaveBeenCalledTimes(1);
  });

  it('sends a past day as local noon, in UTC', async () => {
    renderControls();

    fireEvent.change(screen.getByLabelText('Day'), { target: { value: '2026-10-08' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log' }));

    await waitFor(() => expect(logCook).toHaveBeenCalledWith(ID, new Date(2026, 9, 8, 12, 0).toISOString()));
  });

  it('sends no date when the chosen day is today, since noon may still be ahead', async () => {
    renderControls();

    fireEvent.change(screen.getByLabelText('Day'), { target: { value: '2026-10-11' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log' }));

    await waitFor(() => expect(logCook).toHaveBeenCalledWith(ID, undefined));
  });

  it('does not offer a future day', () => {
    renderControls();

    expect(screen.getByLabelText('Day').getAttribute('max')).toBe('2026-10-11');
  });

  it('says so when logging fails, without the server message', async () => {
    logCook.mockRejectedValue(new Error('Npgsql: connection refused'));
    renderControls();

    fireEvent.click(screen.getByRole('button', { name: 'I cooked this' }));

    expect((await screen.findByRole('alert')).textContent).toBe("Couldn't log this cook. Try again.");
  });
});
