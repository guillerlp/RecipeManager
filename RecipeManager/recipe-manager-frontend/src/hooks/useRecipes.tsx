import { recipeService } from "@/services"
import type { Recipe, RecipeStatus } from "@/types"
import { useQuery } from "@tanstack/react-query"

// ['recipes', 'list', status]: the 'list' segment keeps list keys apart from useRecipe's ['recipes', id]. Both sit
// under the ['recipes'] prefix every mutation invalidates (useRecipeMutations.ts).
export const useRecipes = (status: RecipeStatus = 'Published') => {
    return useQuery({
        queryKey: ['recipes', 'list', status],
        queryFn: async (): Promise<Recipe[]> => {
            const {data} = await recipeService.getAllRecipes(status);
            return data;
        },
        staleTime: 5 * 60 * 1000,
        gcTime: 15 * 60 * 1000,
        retry: 2,
        refetchOnWindowFocus: false,
        // Refetches on mount only when stale: within staleTime that means only after a write invalidated it, which is
        // how a screen that was not mounted during the write (Home after a cook on Detail) picks the write up.
        refetchOnMount: true,
        refetchOnReconnect: false,

        throwOnError: false
    });
};

export const useRecipesLoading = () => {
    const { isLoading, isFetching } = useRecipes();
    return isLoading || isFetching;
}
