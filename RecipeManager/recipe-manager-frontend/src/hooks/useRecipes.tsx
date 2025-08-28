import { recipeService } from "@/services"
import { Recipe } from "@/types"
import { useQuery } from "@tanstack/react-query"

export const useRecipes = () => {
    return useQuery({
        queryKey: ['recipes'],
        queryFn: async (): Promise<Recipe[]> => {
            const {data} = await recipeService.getAllRecipes();
            return data;
        },
        staleTime: 5 * 60 * 1000,
        gcTime: 15 * 60 * 1000,
        retry: 2,
        refetchOnWindowFocus: false,
        refetchOnMount: false,
        refetchOnReconnect: false,

        throwOnError: false
    });
};

export const useRecipesLoading = () => {
    const { isLoading, isFetching } = useRecipes();
    return isLoading || isFetching;
}