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

    public void Dispose()
    {
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
