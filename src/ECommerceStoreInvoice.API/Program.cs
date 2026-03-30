using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.API.Endpoints;
using ECommerceStoreInvoice.Application;
using ECommerceStoreInvoice.Domain;
using ECommerceStoreInvoice.Infrastructure;
using ECommerceStoreInvoice.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApiDocument(options =>
{
    options.PostProcess = document =>
    {
        foreach (var schema in document.Components.Schemas.Values)
        {
            FixGuidFormats(schema);
        }
    };
});

builder.Services.AddHealthChecks();

builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDomain();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

await app.Services.InitializeInfrastructureAsync();

app.UseExceptionHandler();

app.UseOpenApi();
app.UseSwaggerUi();
app.UseHttpsRedirection();

app.MapInvoicesEndpoints();
app.MapProductVersionsEndpoints();
app.MapOrdersEndpoints();
app.MapShoppingCartEndpoints();
app.MapShoppingCartFlowDescriptorsEndpoints();

app.MapHealthChecks("/health");

app.Run();




static void FixGuidFormats(NJsonSchema.JsonSchema schema)
{
    if (schema.Type.HasFlag(NJsonSchema.JsonObjectType.String) &&
        string.Equals(schema.Format, "guid", StringComparison.OrdinalIgnoreCase))
    {
        schema.Format = "uuid";
    }

    foreach (var property in schema.Properties.Values)
    {
        FixGuidFormats(property);
    }

    if (schema.Item is not null)
    {
        FixGuidFormats(schema.Item);
    }

    foreach (var allOf in schema.AllOf)
    {
        FixGuidFormats(allOf);
    }

    foreach (var anyOf in schema.AnyOf)
    {
        FixGuidFormats(anyOf);
    }

    foreach (var oneOf in schema.OneOf)
    {
        FixGuidFormats(oneOf);
    }
}