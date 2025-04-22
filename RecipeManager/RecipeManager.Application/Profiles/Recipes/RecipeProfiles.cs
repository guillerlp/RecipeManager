using AutoMapper;
using RecipeManager.Domain.Entities;
using RecipeManager.Application.DTO.Recipes;

namespace RecipeManager.Application.Profiles.Recipes
{
    public class RecipeProfiles : Profile
    {
        public RecipeProfiles()
        {
            CreateMap<Recipe, RecipeDto>();
        }
    }
}
