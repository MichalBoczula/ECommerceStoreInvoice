using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Health;

public class MongoReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenMongoIsUnavailable_ShouldReportUnhealthy()
    {
        var client = new Mock<IMongoClient>();
        client.Setup(x => x.GetDatabase("admin", null))
            .Throws(new TimeoutException("MongoDB unavailable"));
        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "invoices",
            ShoppingCartsCollectionName = "shoppingCarts",
            OrdersCollectionName = "orders",
            ProductVersionsCollectionName = "productVersions",
            InvoicesCollectionName = "invoices",
            ClientDataVersionsCollectionName = "clientDataVersions"
        });
        var check = new MongoReadinessHealthCheck(client.Object, settings);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("MongoDB is unavailable for transactions.");
        result.Exception.ShouldBeNull();
    }
}
