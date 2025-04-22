using Microsoft.EntityFrameworkCore;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Infrastructure.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<Recipe> Recipes { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
