namespace RecipeManager.Domain.Entities;

// Stored by name, like Unit (ADR-022), so reordering the members cannot remap stored rows.
public enum RecipeStatus
{
    Draft,
    Published
}
