using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Domain.Entities;

namespace RecipeManager.Infrastructure.Context.Configurations;

/// <summary>
/// The first entity configuration in this codebase (ADR-022). Mapping before it was entirely
/// convention-based, which is why Title and Description are still unbounded text (SEC-08); the owned
/// ingredient and instruction-step columns are bounded here.
/// </summary>
public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        // Entity.Id is minted by the domain (Guid.NewGuid() in the entity's private constructor), never by
        // the database, so EF must not treat a set key as evidence the row already exists. Without this,
        // EF's convention for a Guid key is ValueGeneratedOnAdd, which makes a set-but-unsaved key look
        // exactly like an existing row: SaveChanges paints the entity Modified instead of Added, and the
        // resulting UPDATE affects 0 rows.
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.OwnsMany(r => r.Ingredients, ingredient =>
        {
            ingredient.ToTable("RecipeIngredients");
            ingredient.HasKey(i => i.Id);
            // Same reasoning as Recipe.Id above: Ingredient.Create also mints its own Guid client-side.
            ingredient.Property(i => i.Id).ValueGeneratedNever();
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

        builder.OwnsMany(r => r.Instructions, step =>
        {
            step.ToTable("RecipeInstructionSteps");
            step.HasKey(s => s.Id);
            // Same reasoning as Recipe.Id above: InstructionStep.Create mints its own Guid client-side.
            step.Property(s => s.Id).ValueGeneratedNever();
            step.Property(s => s.Position).IsRequired();
            step.Property(s => s.Text).IsRequired().HasMaxLength(2000);
            // A uuid[] primitive collection, not a join table (ADR-022): step-to-ingredient integrity is a
            // domain invariant in Recipe.ValidateProperties. Field access because the getter returns a copy.
            step.PrimitiveCollection(s => s.IngredientIds)
                .IsRequired()
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Instructions is a computed getter (it sorts), so EF must read and write the backing field instead.
        builder.Navigation(r => r.Instructions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
