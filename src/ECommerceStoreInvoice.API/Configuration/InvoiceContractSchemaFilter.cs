using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerceStoreInvoice.API.Configuration;

/// <summary>Describes actual serializer output and safe problem writers without changing binding.</summary>
public sealed class InvoiceContractSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is null)
            return;
        var example = InvoiceContractExamples.For(context.Type);
        if (example is null) return;
        var request = context.Type.Namespace?.Contains(".RequestsDto", StringComparison.Ordinal) == true;
        if (request)
        {
            concrete.AdditionalPropertiesAllowed = true;
            concrete.AdditionalProperties = null;
        }
        else
            concrete.Required = example.Select(property => property.Key).ToHashSet(StringComparer.Ordinal);
        if (typeof(ProblemDetails).IsAssignableFrom(context.Type))
        {
            concrete.AdditionalPropertiesAllowed = false;
            concrete.AdditionalProperties = null;
            if (!concrete.Properties.ContainsKey("traceId"))
                concrete.Properties["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String };
            foreach (var property in concrete.Properties.Values.OfType<OpenApiSchema>())
                property.Type &= ~JsonSchemaType.Null;
            foreach (var (name, description) in new[]
            {
                ("type", "Absolute URI identifying the HTTP problem category."),
                ("title", "Safe human-readable problem title."),
                ("status", "HTTP int32 status code, equal to the actual response status."),
                ("detail", "Safe explanation without internal exception, stack or storage details."),
                ("instance", "Request path that produced this problem."),
                ("traceId", "Non-null request correlation identifier.")
            }) concrete.Properties[name].Description = description;
            concrete.Description ??= "Complete safe six-field HTTP problem, including traceId. Business validation adds its required errors array.";
            if (concrete.Properties["type"] is OpenApiSchema category) category.Format = "uri";
        }
        concrete.Example = example.DeepClone();
        foreach (var (name, value) in example)
            if (concrete.Properties.TryGetValue(name, out var property) && property is OpenApiSchema field)
                field.Example = value?.DeepClone();
    }
}
