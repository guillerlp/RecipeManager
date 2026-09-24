using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RecipeManager.Api.Startup.Swagger;

/// <summary>
/// OpenAPI 3.0 forbids sibling keywords next to a <c>$ref</c>, so Swashbuckle emits a nullable enum property
/// (e.g. <c>IngredientDto.Unit</c>) as a bare reference with the nullability silently dropped, and
/// <see cref="RequireNonNullablePropertiesSchemaFilter"/> then wrongly lists it in <c>required</c> because
/// nothing on the reference says otherwise. This wraps just those properties' references in <c>allOf</c> so
/// <c>nullable: true</c> can sit beside them — scoped to the properties that actually need it, instead of
/// turning on Swashbuckle's <c>UseAllOfToExtendReferenceSchemas()</c> for the whole document, which also
/// rewraps every request-body <c>$ref</c> that needs no such override.
/// </summary>
public sealed class NullableEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema { Properties.Count: > 0 } concrete)
        {
            return;
        }

        foreach (PropertyInfo property in context.Type.GetProperties())
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) is not { IsEnum: true })
            {
                continue;
            }

            string propertyName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!concrete.Properties.TryGetValue(propertyName, out IOpenApiSchema? reference))
            {
                continue;
            }

            concrete.Properties[propertyName] = new OpenApiSchema
            {
                Type = JsonSchemaType.Null,
                AllOf = [reference],
            };
            concrete.Required?.Remove(propertyName);
        }
    }
}
