﻿namespace RecipeManager.Domain.Shared
{
    public abstract class Entity
    {
        public Guid Id { get; protected init; }

        public override bool Equals(object? obj)
        {
            return obj is Entity other &&
                GetType() == other.GetType() &&
                Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
