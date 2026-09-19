using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RecipeManager.Api.Startup.Swagger
{
    /// <summary>
    /// Lists every non-nullable property in its schema's <c>required</c> array, so the generated TypeScript types
    /// (R-09 / ADR-019) carry no optional markers the server does not justify. Swashbuckle's own
    /// <c>NonNullableReferenceTypesAsRequired()</c> covers reference types only, which would leave <c>Guid</c>
    /// and <c>int</c> members optional. Nullability itself comes from <c>SupportNonNullableReferenceTypes()</c>.
    /// </summary>
    public sealed class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is not OpenApiSchema { Properties.Count: > 0 } concrete)
            {
                return;
            }

            foreach ((string name, IOpenApiSchema property) in concrete.Properties)
            {
                bool isNullable = property.Type is { } type && type.HasFlag(JsonSchemaType.Null);
                if (!isNullable)
                {
                    (concrete.Required ??= new HashSet<string>()).Add(name);
                }
            }
        }
    }
}
