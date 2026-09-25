using ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Configuration;
using Shouldly;

namespace ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Tests;

public sealed class ExternalProductServiceClientTests(ExternalProvidersApplicationFactory factory)
    : IClassFixture<ExternalProvidersApplicationFactory>
{
    [Fact]
    public async Task EmptyIdsDoNotCallProducts()
    {
        var result = await factory.CreateClient().GetProductsByIds([]);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task MissingProductsReturnEmptyCollectionFromHttp404()
    {
        var result = await factory.CreateClient().GetProductsByIds([Guid.NewGuid()]);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistingProductIsMappedFromActualHttpResponse()
    {
        var result = await factory.CreateClient().GetProductsByIds([ExternalProvidersApplicationFactory.ExistingProductId]);
        var snapshot = result.Single();
        snapshot.ProductId.ShouldBe(ExternalProvidersApplicationFactory.ExistingProductId);
        snapshot.Name.ShouldBe("iPhone 15");
        snapshot.Brand.ShouldBe("Apple");
        snapshot.Price.Amount.ShouldBe(4500.50m);
        snapshot.Price.Currency.ShouldBe("PLN");
    }
}
