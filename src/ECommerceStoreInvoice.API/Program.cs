using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.API.Endpoints;
using ECommerceStoreInvoice.Application;
using ECommerceStoreInvoice.Domain;
using ECommerceStoreInvoice.Infrastructure;
using ECommerceStoreInvoice.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApiDocument();
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

app.MapHealthChecks("/health");

app.Run();