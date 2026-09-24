using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeManager.Api;
using RecipeManager.Infrastructure.Context;

namespace RecipeManager.IntegrationTests;

/// <summary>
/// Runs the real ASP.NET Core pipeline through <see cref="WebApplicationFactory{TEntryPoint}"/> against a real
/// PostgreSQL database (ADR-017): a fresh <c>TestDb_{Guid}</c> on the container shared by
/// <see cref="PostgresCollection"/>, one per test class.
/// </summary>
public class IntegrationTestBase : IDisposable
{
    // Mirrors RecipeManager.Api's own AddJsonOptions (ServiceInitializer.cs): the API serialises Unit as a
    // string via JsonStringEnumConverter, so a test reading its responses needs the same converter --
    // default JsonSerializerOptions has none and cannot deserialize an IngredientDto.
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected readonly HttpClient Client;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly IServiceScope Scope;
    protected readonly AppDbContext DbContext;

    protected IntegrationTestBase(PostgresContainerFixture postgres)
    {
        // xUnit 2.x has no dynamic skip (Assert.Skip is v3 only), so Xunit.SkippableFact provides it: this
        // throws, and the [SkippableFact]/[SkippableTheory] attributes turn the failure into a skip.
        Skip.If(postgres.SkipReason is not null, postgres.SkipReason);

        string connectionString = postgres.ConnectionStringFor($"TestDb_{Guid.NewGuid():N}");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTest");

                builder.ConfigureTestServices(services =>
                {
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                    });
                });
            });

        Client = Factory.CreateClient();

        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Creates the database and applies the committed migrations — the same ones a deployment applies, which
        // is why this is Migrate() and not EnsureCreated(). Program skips its own MigrateDatabase() call under
        // the IntegrationTest environment (ADR-005), so the schema is this class's responsibility.
        DbContext.Database.Migrate();
    }

    protected async Task SeedDatabase<T>(params T[] entities) where T : class
    {
        await DbContext.Set<T>().AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}
