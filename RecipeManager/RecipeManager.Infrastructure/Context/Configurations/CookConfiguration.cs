using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Infrastructure.Context.Configurations;

public sealed class CookConfiguration : IEntityTypeConfiguration<Cook>
{
    public void Configure(EntityTypeBuilder<Cook> builder)
    {
        builder.ToTable("Cooks");

        // Minted by the domain, as Recipe.Id — see RecipeConfiguration for why this must not be ValueGeneratedOnAdd.
        builder.Property(c => c.Id).ValueGeneratedNever();

        // A reference by identity with no navigation on either side (ADR-028). The cascade is the whole of
        // "a recipe's cooks die with it": one DELETE, atomic in PostgreSQL, so ADR-006 needs no unit of work.
        // EF indexes the FK column by convention (IX_Cooks_RecipeId), which serves GetStatsAsync's GROUP BY.
        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(c => c.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
