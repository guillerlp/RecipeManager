import { isNotFoundError, recipeService } from '@/services';
import type { Recipe } from '@/types';
import { useQuery } from '@tanstack/react-query';

// ['recipes', id] sits under the list's ['recipes'] prefix, so the first mutation hook (R-21)
// invalidates both with one invalidateQueries({ queryKey: ['recipes'] }).
export const useRecipe = (id: string) =>
  useQuery({
    queryKey: ['recipes', id],
    queryFn: async (): Promise<Recipe> => {
      const { data } = await recipeService.getRecipeById(id);
      return data;
    },
    staleTime: 5 * 60 * 1000,
    gcTime: 15 * 60 * 1000,
    // A missing recipe will still be missing on the next attempt; retrying only delays "not found".
    retry: (failureCount, error) => !isNotFoundError(error) && failureCount < 2,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
    refetchOnReconnect: false,
  });
