using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.API.Endpoints;
using ECommerceStoreInvoice.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApiDocument();
builder.Services.AddHealthChecks();

builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDomain();

var app = builder.Build();

app.UseExceptionHandler();

app.UseOpenApi();
app.UseSwaggerUi();
app.UseHttpsRedirection();

app.MapInvoicesEndpoints();
app.MapProductVersionsEndpoints();

app.MapHealthChecks("/health");

app.Run();