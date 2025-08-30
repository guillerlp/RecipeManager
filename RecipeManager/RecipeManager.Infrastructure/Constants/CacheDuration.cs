namespace RecipeManager.Infrastructure.Constants;

public static class CacheDuration
{
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan DefaultSliding = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan LongExpiration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan LongSliding = TimeSpan.FromMinutes(15);
}