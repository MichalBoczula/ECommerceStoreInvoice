using System.Net;
using System.Text.Json;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Concret.Products;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Configuration;

// Real Kiota HTTP serialization against an isolated in-process Products endpoint.
// No locally built Products image or SQL Server is required for this contract test.
public sealed class ExternalProvidersApplicationFactory : IDisposable
{
    public static readonly Guid ExistingProductId = Guid.Parse("0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2");
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public ExternalProvidersApplicationFactory()
    {
        _server = new TestServer(new WebHostBuilder().Configure(app => app.Run(async context =>
        {
            if (context.Request.Method != "POST" || context.Request.Path != "/mobile-phones/by-ids")
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            var ids = await JsonSerializer.DeserializeAsync<Guid[]>(context.Request.Body);
            if (ids is null || !ids.Contains(ExistingProductId))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync("{\"title\":\"Not found\",\"status\":404}");
                return;
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"[{{\"id\":\"{ExistingProductId}\",\"name\":\"iPhone 15\",\"brand\":\"Apple\",\"price\":{{\"amount\":4500.50,\"currency\":\"PLN\"}}}}]");
        })));

        _client = _server.CreateClient();
        _client.BaseAddress = new Uri("http://localhost");
    }

    public IProductServiceClient CreateClient()
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: _client)
        {
            BaseUrl = _client.BaseAddress!.ToString().TrimEnd('/')
        };
        return new ExternalProductServiceClient(new ProductApiClient(adapter));
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }
}
