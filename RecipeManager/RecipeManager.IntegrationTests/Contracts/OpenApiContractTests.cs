using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using RecipeManager.Api;
using Swashbuckle.AspNetCore.Swagger;

namespace RecipeManager.IntegrationTests.Contracts;

/// <summary>
/// Guards the C# → OpenAPI link of the contract gate (R-09 / ADR-019). No database and no container: the
/// IntegrationTest environment skips DbContext registration and migrations, and Swashbuckle builds the document
/// from ApiExplorer metadata without constructing a controller, so nothing ever resolves the repository.
/// That is why these are plain [Fact]s — they must never skip.
/// </summary>
public class OpenApiContractTests : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("IntegrationTest"));

    // The Swagger middleware is mapped only in Development (ApplicationInitializer.SetupSwagger), so the test
    // reads the document from the provider the middleware itself uses instead of over HTTP.
    protected OpenApiDocument GetDocument() =>
        Factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

    [Theory]
    [InlineData("RecipeDto")]
    [InlineData("UpdateRecipeDto")]
    [InlineData("CreateRecipeCommand")]
    public void RecipeSchemas_ShouldMarkEveryPropertyRequired(string schemaName)
    {
        // ==================== ARRANGE ====================
        OpenApiDocument document = GetDocument();

        // ==================== ACT ====================
        IOpenApiSchema schema = document.Components!.Schemas![schemaName];

        // ==================== ASSERT ====================
        // A member missing from `required` becomes an optional `?` property in the generated TypeScript, which is
        // drift the generator would reproduce faithfully.
        schema.Properties.Should().NotBeNullOrEmpty();
        schema.Required.Should().BeEquivalentTo(schema.Properties!.Keys,
            "every member of {0} is non-nullable in C#, so the client may rely on it being present", schemaName);
    }

    private const string UpdateVariable = "UPDATE_OPENAPI_SNAPSHOT";

    // Named both ways an outdated snapshot can be accepted: running the update switch where the integration
    // host can actually start, or — where it can't (e.g. Windows Smart App Control, INFRA-06) — promoting the
    // *.received.json file this test leaves behind (CI uploads the same file as the `openapi-received`
    // artifact when the assertion below fails there).
    private const string AcceptChangeHint =
        "accept the change by running `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test --filter OpenApiContractTests` " +
        "locally, or, where the test cannot run locally (e.g. Windows Smart App Control, INFRA-06), by copying " +
        "contracts/openapi.received.json — also uploaded by CI as the `openapi-received` artifact — over " +
        "contracts/openapi.json, then running `npm run gen:api` in recipe-manager-frontend/ and committing both " +
        "files";

    [Fact]
    public async Task OpenApiDocument_ShouldMatchCommittedSnapshot()
    {
        // ==================== ARRANGE ====================
        string snapshotPath = SnapshotPath();
        string receivedPath = Path.ChangeExtension(snapshotPath, ".received.json");

        // ==================== ACT ====================
        string actual = Normalize(await GetDocument().SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0));

        // Golden-master update switch for developers. Never set in CI, where it would make this test pass by
        // rewriting its own expectation.
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, actual);
            File.Delete(receivedPath);
            return;
        }

        // "Received file" snapshot pattern (as Verify's *.received.* files): a mismatch is written next to the
        // committed snapshot before asserting, so a machine that cannot run this test at all still leaves
        // something a developer — or CI, via the uploaded artifact — can diff or promote by hand.
        string? expected = File.Exists(snapshotPath) ? Normalize(await File.ReadAllTextAsync(snapshotPath)) : null;
        if (actual == expected)
        {
            File.Delete(receivedPath); // No-throw when absent — a pass leaves no stale received file behind.
            return;
        }

        await File.WriteAllTextAsync(receivedPath, actual);

        // ==================== ASSERT ====================
        File.Exists(snapshotPath).Should().BeTrue(
            "the contract snapshot must be committed at {0}; {1}", snapshotPath, AcceptChangeHint);
        actual.Should().Be(expected, "the API contract changed; {0}", AcceptChangeHint);
    }

    // Line endings normalised so a Windows checkout (core.autocrlf) and the Linux runner compare equal.
    private static string Normalize(string json) => json.ReplaceLineEndings("\n").TrimEnd() + "\n";

    // Walks up from bin/<config>/net10.0 to the folder holding RecipeManager.sln.
    private static string SnapshotPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RecipeManager.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the test must run from inside the solution folder");
        return Path.Combine(directory!.FullName, "contracts", "openapi.json");
    }

    public void Dispose()
    {
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
