using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.API.Endpoints;
using ECommerceStoreInvoice.Application;
using ECommerceStoreInvoice.Domain;
using ECommerceStoreInvoice.Infrastructure;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(builder.Configuration["InvoiceStorage:ConnectionString"]))
    throw new InvalidOperationException("InvoiceStorage:ConnectionString is required in Production for durable invoice PDFs.");

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

AddExternalProductsClient(builder);

builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SupportNonNullableReferenceTypes();
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddDomain();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

if (!app.Environment.IsEnvironment("OpenApiExport"))
    await app.Services.InitializeInfrastructureAsync();

app.UseExceptionHandler();
app.UseStatusCodePages(status =>
{
    var context = status.HttpContext;
    if (context.Response.StatusCode is 404 or 405 or 415 &&
        !context.Request.Path.StartsWithSegments("/health") &&
        !context.Request.Path.StartsWithSegments("/swagger"))
        return FrameworkProblemResponse.WriteAsync(context, context.Response.StatusCode, context.RequestAborted);
    return Task.CompletedTask;
});

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapInvoicesEndpoints();
app.MapOrdersEndpoints();
app.MapShoppingCartEndpoints();
app.MapClientDataVersionsEndpoints();
app.MapDocumentationEndpoints();
var liveOptions = new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
};
app.MapHealthChecks("/health", liveOptions);
app.MapHealthChecks("/health/live", liveOptions);
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();

static void AddExternalProductsClient(WebApplicationBuilder builder)
{
    var productCatalogUrl = builder.Configuration["ExternalServices:ProductCatalog:BaseUrl"];
    if (string.IsNullOrWhiteSpace(productCatalogUrl))
    {
        throw new InvalidOperationException(
            "HttpClient.BaseAddress for ProductApiClient is not configured. " +
            "Ensure 'ExternalServices:ProductCatalog:BaseUrl' is provided in configuration.");
    }

    builder.Services.AddHttpClient<ProductApiClient>(client =>
    {
        client.BaseAddress = new Uri(productCatalogUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddTypedClient<ProductApiClient>((httpClient, sp) =>
    {
        var authProvider = new AnonymousAuthenticationProvider();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = httpClient.BaseAddress!.ToString().TrimEnd('/')
        };

        return new ProductApiClient(adapter);
    });
}

public partial class Program { }
