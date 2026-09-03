using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.API.Endpoints;
using ECommerceStoreInvoice.Application;
using ECommerceStoreInvoice.Domain;
using ECommerceStoreInvoice.Infrastructure;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

AddExternalProductsClient(builder);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SupportNonNullableReferenceTypes();
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

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapInvoicesEndpoints();
app.MapOrdersEndpoints();
app.MapShoppingCartEndpoints();
app.MapClientDataVersionsEndpoints();
app.MapDocumentationEndpoints();
app.MapHealthChecks("/health");

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