using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Shouldly;
using MongoDB.Bson;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Health;

public class MongoReadinessHealthCheckTests
{
    [Theory]
    [InlineData("{ setName: 'rs0', isWritablePrimary: true, logicalSessionTimeoutMinutes: 30 }", true)]
    [InlineData("{ msg: 'isdbgrid', isWritablePrimary: true, logicalSessionTimeoutMinutes: 30 }", true)]
    [InlineData("{ isWritablePrimary: true, logicalSessionTimeoutMinutes: 30 }", false)]
    [InlineData("{ msg: 'isdbgrid', isWritablePrimary: false, logicalSessionTimeoutMinutes: 30 }", false)]
    [InlineData("{ setName: 'rs0', isWritablePrimary: true }", false)]
    [InlineData("{ msg: 'isdbgrid', isWritablePrimary: true, logicalSessionTimeoutMinutes: 0 }", false)]
    [InlineData("{ msg: 'isdbgrid', isWritablePrimary: true, logicalSessionTimeoutMinutes: '30' }", false)]
    public void TransactionTopologyRequiresWritableReplicaOrRouterAndSessions(string hello, bool expected) =>
        MongoReadinessHealthCheck.SupportsTransactionTopology(BsonDocument.Parse(hello)).ShouldBe(expected);

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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAtHelloOrPingIsPropagatedWithoutAnUnhealthyFallback(bool atPing)
    {
        using var cancellation = new CancellationTokenSource();
        var client = new Mock<IMongoClient>();
        var admin = new Mock<IMongoDatabase>();
        var database = new Mock<IMongoDatabase>();
        client.Setup(c => c.GetDatabase("admin", null)).Returns(admin.Object);
        client.Setup(c => c.GetDatabase("invoices", null)).Returns(database.Object);
        admin.Setup(d => d.RunCommandAsync<BsonDocument>(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), cancellation.Token))
            .Returns(() =>
            {
                if (atPing) return Task.FromResult(BsonDocument.Parse("{setName:'rs0',isWritablePrimary:true,logicalSessionTimeoutMinutes:30}"));
                cancellation.Cancel();
                return Task.FromCanceled<BsonDocument>(cancellation.Token);
            });
        database.Setup(d => d.RunCommandAsync<BsonDocument>(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), cancellation.Token))
            .Returns(() => { cancellation.Cancel(); return Task.FromCanceled<BsonDocument>(cancellation.Token); });
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

        await Should.ThrowAsync<OperationCanceledException>(() => check.CheckHealthAsync(new HealthCheckContext(), cancellation.Token));

        cancellation.IsCancellationRequested.ShouldBeTrue();
        admin.Verify(d => d.RunCommandAsync<BsonDocument>(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), cancellation.Token), Times.Once());
        database.Verify(d => d.RunCommandAsync<BsonDocument>(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), cancellation.Token), atPing ? Times.Once() : Times.Never());
        client.Verify(c => c.GetDatabase("invoices", null), atPing ? Times.Once() : Times.Never());
    }

}
