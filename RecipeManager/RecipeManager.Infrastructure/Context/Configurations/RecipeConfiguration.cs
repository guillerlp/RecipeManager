using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Infrastructure.Context.Configurations;

/// <summary>
/// The first entity configuration in this codebase (ADR-022). Until now mapping was entirely
/// convention-based, which is why every string column is unbounded text (SEC-08).
/// </summary>
public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.OwnsMany(r => r.Ingredients, ingredient =>
        {
            ingredient.ToTable("RecipeIngredients");
            ingredient.HasKey(i => i.Id);
            ingredient.Property(i => i.Position).IsRequired();
            ingredient.Property(i => i.Quantity).HasPrecision(9, 3);
            // Stored as the member name, not its ordinal: reordering the enum then cannot silently remap
            // existing rows, and the column is readable in psql.
            ingredient.Property(i => i.Unit).HasConversion<string>().HasMaxLength(20);
            ingredient.Property(i => i.Name).IsRequired().HasMaxLength(200);
            ingredient.Property(i => i.Notes).HasMaxLength(200);
        });

        // Ingredients is a computed getter (it sorts), so EF must read and write the backing field instead.
        builder.Navigation(r => r.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
