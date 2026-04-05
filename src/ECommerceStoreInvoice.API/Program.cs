using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.API.Endpoints;
using ECommerceStoreInvoice.API.Configuration.Extensions;
using ECommerceStoreInvoice.Application;
using ECommerceStoreInvoice.Domain;
using ECommerceStoreInvoice.Infrastructure;
using ECommerceStoreInvoice.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApiDocument(options =>
{
    options.PostProcess = document =>
    {
        foreach (var schema in document.Components.Schemas.Values)
        {
            schema.FixGuidFormats();
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
app.MapDocumentationEndpoints();

app.MapHealthChecks("/health");

app.Run();