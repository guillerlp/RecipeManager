// The SPA's first mutations (spec 015 §8.4). One rule for all five: on success, invalidate the ['recipes'] prefix.
// It covers every list (['recipes', 'list', status]) and every detail entry (['recipes', id]); a status change
// moves a recipe between lists, so nothing narrower is safe. useRecipes and useRecipe set refetchOnMount: false,
// so without this the screens would keep showing the old recipe until a reload.
import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { recipeService } from '@/services';
import type { CreateRecipeRequest, Recipe, UpdateRecipeRequest } from '@/types';

// Returned from onSuccess, so mutateAsync resolves only once the refetch has been triggered.
const invalidateRecipes = (queryClient: QueryClient) => queryClient.invalidateQueries({ queryKey: ['recipes'] });

export const useCreateRecipe = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateRecipeRequest): Promise<Recipe> => (await recipeService.createRecipe(request)).data,
    onSuccess: created => {
      // The form redirects to /recipes/{id}/edit next; seeding saves that screen a fetch and a loading flash.
      queryClient.setQueryData(['recipes', created.id], created);
      return invalidateRecipes(queryClient);
    },
  });
};

export const useUpdateRecipe = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateRecipeRequest }): Promise<void> => {
      await recipeService.updateRecipe(id, request);
    },
    onSuccess: () => invalidateRecipes(queryClient),
  });
};

export const usePublishRecipe = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      await recipeService.publishRecipe(id);
    },
    onSuccess: () => invalidateRecipes(queryClient),
  });
};

export const useUnpublishRecipe = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      await recipeService.unpublishRecipe(id);
    },
    onSuccess: () => invalidateRecipes(queryClient),
  });
};

export const useDeleteRecipe = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string): Promise<void> => {
      await recipeService.deleteRecipe(id);
    },
    onSuccess: (_result, id) => {
      // Removed first: an invalidated detail entry with an observer would be refetched straight into a 404.
      queryClient.removeQueries({ queryKey: ['recipes', id], exact: true });
      return invalidateRecipes(queryClient);
    },
  });
};
