import { isNotFoundError, recipeService } from '@/services';
import type { Recipe } from '@/types';
import { useQuery } from '@tanstack/react-query';

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// The id comes from the URL, and React Router decodes %2F in a param: "..%2FRecipes" arrives as
// "../Recipes" and would be resolved by the browser into a different API path. Only a GUID is sent.
export const isRecipeId = (id: string): boolean => GUID.test(id);

// ['recipes', id] sits under the list's ['recipes'] prefix, so the first mutation hook (R-21)
// invalidates both with one invalidateQueries({ queryKey: ['recipes'] }).
export const useRecipe = (id: string) =>
  useQuery({
    queryKey: ['recipes', id],
    enabled: isRecipeId(id),
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
