import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import type { AxiosResponse } from 'axios';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { recipeService } from '@/services';
import type { Recipe, UpdateRecipeRequest } from '@/types';
import {
  useCreateRecipe, useDeleteRecipe, usePublishRecipe, useUnpublishRecipe, useUpdateRecipe,
} from './useRecipeMutations';

vi.mock('@/services', () => ({
  recipeService: {
    createRecipe: vi.fn(), updateRecipe: vi.fn(), publishRecipe: vi.fn(), unpublishRecipe: vi.fn(), deleteRecipe: vi.fn(),
  },
}));

const service = vi.mocked(recipeService);
const ID = '11111111-1111-1111-1111-111111111111';
const request: UpdateRecipeRequest = { title: 'T', ingredients: [], instructions: [], tags: [] };
const created: Recipe = { id: ID, title: 'T', status: 'Draft', ingredients: [], instructions: [], tags: [] };
const ok = <T,>(data: T) => ({ data }) as AxiosResponse<T>;

const setup = () => {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, 'invalidateQueries');
  const wrapper = ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  return { client, invalidate, wrapper };
};

beforeEach(() => {
  vi.resetAllMocks();
  service.createRecipe.mockResolvedValue(ok(created));
  service.updateRecipe.mockResolvedValue(ok(undefined));
  service.publishRecipe.mockResolvedValue(ok(undefined));
  service.unpublishRecipe.mockResolvedValue(ok(undefined));
  service.deleteRecipe.mockResolvedValue(ok(undefined));
});

describe('recipe mutations', () => {
  // useRecipes and useRecipe never refetch by themselves (refetchOnMount: false), so a write that forgets this
  // leaves the list and the detail screen showing the old recipe until a reload.
  type Wrapper = ReturnType<typeof setup>['wrapper'];
  // Each entry renders its hook (outside act, so result.current exists) and returns the write to run.
  const writes: [string, (wrapper: Wrapper) => () => Promise<unknown>][] = [
    ['create', wrapper => {
      const { result } = renderHook(() => useCreateRecipe(), { wrapper });
      return () => result.current.mutateAsync({ ...request, status: 'Draft' });
    }],
    ['update', wrapper => {
      const { result } = renderHook(() => useUpdateRecipe(), { wrapper });
      return () => result.current.mutateAsync({ id: ID, request });
    }],
    ['publish', wrapper => {
      const { result } = renderHook(() => usePublishRecipe(), { wrapper });
      return () => result.current.mutateAsync(ID);
    }],
    ['unpublish', wrapper => {
      const { result } = renderHook(() => useUnpublishRecipe(), { wrapper });
      return () => result.current.mutateAsync(ID);
    }],
    ['delete', wrapper => {
      const { result } = renderHook(() => useDeleteRecipe(), { wrapper });
      return () => result.current.mutateAsync(ID);
    }],
  ];

  it.each(writes)('%s invalidates the whole recipes prefix', async (_name, prepare) => {
    const { invalidate, wrapper } = setup();
    const write = prepare(wrapper);
    await act(async () => { await write(); });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['recipes'] });
  });

  it('create seeds the new recipe so its edit screen needs no fetch', async () => {
    const { client, wrapper } = setup();
    const { result } = renderHook(() => useCreateRecipe(), { wrapper });
    await act(async () => { await result.current.mutateAsync({ ...request, status: 'Draft' }); });
    expect(client.getQueryData(['recipes', ID])).toEqual(created);
  });

  it('delete drops the detail entry instead of refetching it into a 404', async () => {
    const { client, wrapper } = setup();
    client.setQueryData(['recipes', ID], created);
    const { result } = renderHook(() => useDeleteRecipe(), { wrapper });
    await act(async () => { await result.current.mutateAsync(ID); });
    expect(client.getQueryCache().find({ queryKey: ['recipes', ID], exact: true })).toBeUndefined();
    expect(service.deleteRecipe).toHaveBeenCalledWith(ID);
  });

  it('update and the transitions pass the id through', async () => {
    const { wrapper } = setup();
    const update = renderHook(() => useUpdateRecipe(), { wrapper }).result;
    const publish = renderHook(() => usePublishRecipe(), { wrapper }).result;
    const unpublish = renderHook(() => useUnpublishRecipe(), { wrapper }).result;
    await act(async () => {
      await update.current.mutateAsync({ id: ID, request });
      await publish.current.mutateAsync(ID);
      await unpublish.current.mutateAsync(ID);
    });
    expect(service.updateRecipe).toHaveBeenCalledWith(ID, request);
    expect(service.publishRecipe).toHaveBeenCalledWith(ID);
    expect(service.unpublishRecipe).toHaveBeenCalledWith(ID);
  });
});
