using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests;

public sealed class InvoiceGenerationRepositoryTests(MongoDbTestFixture fixture) : IClassFixture<MongoDbTestFixture>
{
    [Fact]
    public async Task ConcurrentClaimsReserveOneOrderAndHideIncompleteInvoice()
    {
        var databaseName = $"invoice-generation-{Guid.NewGuid():N}";
        await using var services = TestServiceProviderFactory.Create(fixture.ConnectionString, databaseName);
        await services.InitializeInfrastructureAsync();
        var generation = services.GetRequiredService<IInvoiceGenerationRepository>();
        var invoices = services.GetRequiredService<IInvoiceRepository>();
        var orderId = Guid.NewGuid();

        var attempts = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => generation.TryClaimAsync(new Invoice(orderId, Guid.NewGuid(), string.Empty), Guid.NewGuid())));

        var claim = attempts.Single(result => result is not null)!;
        (await invoices.GetInvoiceByOrderId(orderId)).ShouldBeNull();
        (await invoices.GetInvoiceById(claim.InvoiceId)).ShouldBeNull();
        var completed = await generation.CompleteAsync(claim, "file:///invoices/winner.pdf");
        (await invoices.GetInvoiceByOrderId(orderId))!.Id.ShouldBe(completed.Id);
        (await invoices.GetInvoiceById(claim.InvoiceId))!.StorageUrl.ShouldBe("file:///invoices/winner.pdf");
        (await generation.TryClaimAsync(new Invoice(orderId, Guid.NewGuid(), string.Empty), Guid.NewGuid())).ShouldBeNull();
    }

    [Fact]
    public async Task FailedClaimCanBeRetriedWithoutAllowingOldAttemptToComplete()
    {
        var databaseName = $"invoice-generation-{Guid.NewGuid():N}";
        await using var services = TestServiceProviderFactory.Create(fixture.ConnectionString, databaseName);
        await services.InitializeInfrastructureAsync();
        var generation = services.GetRequiredService<IInvoiceGenerationRepository>();
        var invoices = services.GetRequiredService<IInvoiceRepository>();
        var orderId = Guid.NewGuid();
        var first = (await generation.TryClaimAsync(new Invoice(orderId, Guid.NewGuid(), string.Empty), Guid.NewGuid()))!;
        await generation.ReleaseAsync(first);

        var second = (await generation.TryClaimAsync(new Invoice(orderId, Guid.NewGuid(), string.Empty), Guid.NewGuid()))!;
        second.InvoiceId.ShouldBe(first.InvoiceId);
        await Should.ThrowAsync<InvalidOperationException>(() => generation.CompleteAsync(first, "file:///stale.pdf"));
        await generation.ReleaseAsync(first);
        (await invoices.GetInvoiceByOrderId(orderId)).ShouldBeNull();
        var invoice = await generation.CompleteAsync(second, "file:///retry.pdf");
        invoice.StorageUrl.ShouldBe("file:///retry.pdf");
    }

    [Fact]
    public async Task ExpiredClaimCanBeReclaimedAfterProcessDies()
    {
        var databaseName = $"invoice-generation-{Guid.NewGuid():N}";
        await using var services = TestServiceProviderFactory.Create(fixture.ConnectionString, databaseName);
        await services.InitializeInfrastructureAsync();
        var generation = services.GetRequiredService<IInvoiceGenerationRepository>();
        var orderId = Guid.NewGuid();
        var first = (await generation.TryClaimAsync(new Invoice(orderId, Guid.NewGuid(), string.Empty), Guid.NewGuid()))!;
        var collection = new MongoClient(fixture.ConnectionString).GetDatabase(databaseName).GetCollection<BsonDocument>("invoices");
        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("OrderId", new BsonBinaryData(orderId, GuidRepresentation.Standard)),
            Builders<BsonDocument>.Update.Set("GenerationLeaseUntil", DateTime.UtcNow.AddMinutes(-1)));

        var second = (await generation.TryClaimAsync(new Invoice(orderId, Guid.NewGuid(), string.Empty), Guid.NewGuid()))!;
        second.InvoiceId.ShouldBe(first.InvoiceId);
        second.AttemptId.ShouldNotBe(first.AttemptId);
        await Should.ThrowAsync<InvalidOperationException>(() => generation.CompleteAsync(first, "file:///stale.pdf"));
        await generation.CompleteAsync(second, "file:///after-crash.pdf");
    }

    [Fact]
    public async Task InvoicesCreatedBeforeGenerationStatusRemainReadable()
    {
        var databaseName = $"invoice-generation-{Guid.NewGuid():N}";
        await using var services = TestServiceProviderFactory.Create(fixture.ConnectionString, databaseName);
        var invoices = services.GetRequiredService<IInvoiceRepository>();
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var collection = new MongoClient(fixture.ConnectionString).GetDatabase(databaseName).GetCollection<BsonDocument>("invoices");
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new BsonBinaryData(invoiceId, GuidRepresentation.Standard),
            ["OrderId"] = new BsonBinaryData(orderId, GuidRepresentation.Standard),
            ["ClientDataVersionId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["StorageUrl"] = "file:///legacy.pdf",
            ["CreatedAt"] = DateTime.UtcNow
        });

        (await invoices.GetInvoiceById(invoiceId))!.StorageUrl.ShouldBe("file:///legacy.pdf");
        (await invoices.GetInvoiceByOrderId(orderId))!.Id.ShouldBe(invoiceId);
    }
}
