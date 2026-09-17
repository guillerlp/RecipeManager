using Npgsql;
using Testcontainers.PostgreSql;

namespace RecipeManager.IntegrationTests;

/// <summary>
/// One PostgreSQL container for the whole test assembly (ADR-017). Test classes take a database of their own
/// on it rather than a container of their own, so the run pays a single container start no matter how many
/// test classes exist.
/// <para>
/// A container that cannot start is recorded as a <see cref="SkipReason"/> instead of throwing: without Docker
/// the integration tests skip and the unit tests still run. That is a deliberate trade — a skipped test is a
/// silent test — and it is safe only because CI always has Docker, so the skip path can never be the one a
/// merge decision rests on.
/// </para>
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    /// <summary>
    /// Pinned to the version <c>README.md</c> installs locally. Dependabot does not manage this string, so it
    /// can drift from production — see ADR-017's consequences.
    /// </summary>
    private const string PostgresImage = "postgres:18-alpine";

    private PostgreSqlContainer? _container;

    /// <summary>
    /// <c>null</c> when the container is running. Otherwise the reason the integration tests cannot run,
    /// phrased for whoever reads the test output.
    /// </summary>
    public string? SkipReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Build() is what probes the Docker endpoint, so it belongs inside the guard as much as StartAsync
            // does — without Docker it throws before a container object ever exists.
            _container = new PostgreSqlBuilder(PostgresImage).Build();
            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            SkipReason =
                $"Docker is unavailable, so PostgreSQL could not start ({exception.GetType().Name}: {exception.Message}). "
                + "The integration tests require Docker — see ADR-017.";
        }
    }

    public Task DisposeAsync() => _container?.DisposeAsync().AsTask() ?? Task.CompletedTask;

    /// <summary>
    /// Connection string for a database of the caller's own on the shared container. Only valid while
    /// <see cref="SkipReason"/> is <c>null</c>.
    /// </summary>
    public string ConnectionStringFor(string database)
    {
        PostgreSqlContainer container = _container
            ?? throw new InvalidOperationException(
                $"The PostgreSQL container is not running. Check {nameof(SkipReason)} before asking for a connection string.");

        return new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = database
        }.ConnectionString;
    }
}

/// <summary>
/// Binds <see cref="PostgresContainerFixture"/> to the test classes carrying
/// <c>[Collection(PostgresCollection.Name)]</c>. xUnit creates one fixture instance for the whole collection.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "PostgreSQL container";
}
