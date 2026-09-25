using System.Net;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public sealed class ProductCatalogContainerTests : IClassFixture<ProductCatalogContainerFixture>
{
    private readonly ProductCatalogContainerFixture _fixture;

    public ProductCatalogContainerTests(ProductCatalogContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishedProductsApiStartsWithSqlServer()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseAddress };
        using var response = await client.GetAsync("health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
