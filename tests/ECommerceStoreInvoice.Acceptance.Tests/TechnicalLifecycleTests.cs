using System.Net;
using System.Net.Sockets;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Orders.Concurrency;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests;

/// <summary>Actual Program bootstrap failures are lifecycle tests: no running endpoint can produce their response.</summary>
public sealed class TechnicalLifecycleTests(ApplicationFactory control) : IClassFixture<ApplicationFactory>
{
    [Theory]
    [InlineData("ExternalServices:ProductCatalog:BaseUrl", "Testing", "HttpClient.BaseAddress for ProductApiClient is not configured.")]
    [InlineData("MongoDbSettings:DatabaseName", "Testing", "MongoDbSettings.DatabaseName is not configured.")]
    [InlineData("InvoiceStorage:ConnectionString", "Production", "InvoiceStorage:ConnectionString is required in Production for durable invoice PDFs.")]
    public async Task MissingRequiredConfigurationPreventsActualHostStartup(string key, string environment, string message)
    {
        var database = $"startup-required-{Guid.NewGuid():N}";
        try
        {
            using var candidate = new StartupApplicationFactory(control.MongoConnection, database, environment, new() { [key] = string.Empty });
            var error = await Record.ExceptionAsync(() => Task.Run(() => { using var client = candidate.CreateClient(); }));
            error.ShouldNotBeNull();
            Chain(error).ShouldContain(e => e is InvalidOperationException && e.Message.StartsWith(message, StringComparison.Ordinal));
            candidate.SuccessfulHosts.ShouldBe(0);
        }
        finally { await new MongoClient(control.MongoConnection).DropDatabaseAsync(database); }
    }

    [Fact]
    public async Task UnavailableMongoPreventsActualHostStartupWithoutAnHttpResponse()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var connection = $"mongodb://127.0.0.1:{port}/?directConnection=true&serverSelectionTimeoutMS=500&connectTimeoutMS=250";
        using var candidate = new StartupApplicationFactory(connection, $"startup-offline-{Guid.NewGuid():N}");
        var error = await Record.ExceptionAsync(() => Task.Run(() => { using var client = candidate.CreateClient(); }));
        error.ShouldNotBeNull(); Chain(error).ShouldContain(e => e is TimeoutException);
        candidate.SuccessfulHosts.ShouldBe(0);
    }

    [Theory]
    [InlineData("cart-duplicates", "shoppingCarts", "UX_ShoppingCart_ClientId", 11000)]
    [InlineData("invoice-duplicates", "invoices", "UX_Invoice_OrderId", 11000)]
    [InlineData("index-options", "shoppingCarts", "UX_ShoppingCart_ClientId", 86)]
    public async Task RealIndexFailurePreventsServingAndARepairedNewHostCanStart(string cause, string collectionName, string indexName, int expectedCode)
    {
        var name = $"startup-index-{Guid.NewGuid():N}";
        var client = new MongoClient(control.MongoConnection);
        var database = client.GetDatabase(name);
        try
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var first = collectionName == "shoppingCarts" ? Cart(Guid.NewGuid()) : Invoice(Guid.NewGuid());
            var second = first.DeepClone().AsBsonDocument;
            second["_id"] = Binary(Guid.NewGuid());
            await collection.InsertOneAsync(first);
            if (cause == "index-options")
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("ClientId", 1), new CreateIndexOptions { Name = indexName, Unique = false }));
            else await collection.InsertOneAsync(second);
            var before = await Snapshot(database);
            using (var failing = new StartupApplicationFactory(control.MongoConnection, name))
            {
                var error = await Record.ExceptionAsync(() => Task.Run(() => { using var http = failing.CreateClient(); }));
                error.ShouldNotBeNull();
                var commandError = Chain(error).OfType<MongoCommandException>().ShouldHaveSingleItem();
                commandError.Code.ShouldBe(expectedCode);
                failing.SuccessfulHosts.ShouldBe(0);
            }
            OrderConcurrencySteps.Equal(await Snapshot(database), before);
            // Startup is not a transaction: earlier collection/index setup can survive a later failure.
            var names = await (await database.ListCollectionNamesAsync()).ToListAsync();
            names.ShouldContain("orders"); names.ShouldContain("productVersions");
            if (cause == "invoice-duplicates")
            {
                var cartIndexes = await (await database.GetCollection<BsonDocument>("shoppingCarts").Indexes.ListAsync()).ToListAsync();
                cartIndexes.ShouldContain(d => d["name"] == "UX_ShoppingCart_ClientId" && d["unique"].AsBoolean);
            }
            if (cause == "index-options") await collection.Indexes.DropOneAsync(indexName);
            else (await collection.DeleteOneAsync(new BsonDocument("_id", second["_id"]))).DeletedCount.ShouldBe(1);
            var repaired = await Snapshot(database);
            using (var healthy = new StartupApplicationFactory(control.MongoConnection, name))
            {
                using var http = healthy.CreateClient();
                using var ready = await http.GetAsync("/health/ready");
                ready.StatusCode.ShouldBe(HttpStatusCode.OK); (await ready.Content.ReadAsStringAsync()).ShouldBe("Healthy");
                healthy.SuccessfulHosts.ShouldBe(1);
                OrderConcurrencySteps.Equal(await Snapshot(database), repaired);
                var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
                indexes.ShouldContain(d => d["name"] == indexName && d["unique"].AsBoolean);
                var versions = await (await database.GetCollection<BsonDocument>("clientDataVersions").Indexes.ListAsync()).ToListAsync();
                var versionKey = new BsonDocument { ["ClientId"] = 1, ["CreatedAt"] = -1 };
                versions.ShouldContain(d => d["name"] == "IX_ClientDataVersion_ClientId_CreatedAtDesc" && d["key"].AsBsonDocument.Equals(versionKey));
            }
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    private static IEnumerable<Exception> Chain(Exception error)
    {
        yield return error;
        if (error is AggregateException aggregate)
            foreach (var child in aggregate.InnerExceptions) foreach (var nested in Chain(child)) yield return nested;
        else if (error.InnerException is { } inner) foreach (var nested in Chain(inner)) yield return nested;
    }
    private static async Task<Dictionary<string, BsonDocument[]>> Snapshot(IMongoDatabase database)
    {
        var result = new Dictionary<string, BsonDocument[]>();
        foreach (var name in new[] { "shoppingCarts", "orders", "productVersions", "invoices", "clientDataVersions" })
            result[name] = (await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).OrderBy(d => d["_id"].AsBsonBinaryData.ToGuid()).ToArray();
        return result;
    }
    private static BsonDocument Cart(Guid id) => new()
    {
        ["_id"] = Binary(id),
        ["ClientId"] = Binary(Guid.NewGuid()),
        ["CreatedAt"] = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ["UpdatedAt"] = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        ["Lines"] = new BsonArray { new BsonDocument { ["ProductId"] = Binary(Guid.NewGuid()), ["Quantity"] = 2 } }
    };
    private static BsonDocument Invoice(Guid id) => new()
    {
        ["_id"] = Binary(id),
        ["OrderId"] = Binary(Guid.NewGuid()),
        ["ClientDataVersionId"] = Binary(Guid.NewGuid()),
        ["StorageUrl"] = "file:///fixtures/startup.pdf",
        ["CreatedAt"] = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc),
        ["GenerationStatus"] = "Completed",
        ["GenerationAttemptId"] = BsonNull.Value,
        ["GenerationLeaseUntil"] = BsonNull.Value
    };
    private static BsonBinaryData Binary(Guid id) => new(id, GuidRepresentation.Standard);
}

internal sealed class StartupApplicationFactory(string connection, string database, string environment = "Testing", Dictionary<string, string?>? overrides = null) : WebApplicationFactory<Program>
{
    internal int SuccessfulHosts { get; private set; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        foreach (var pair in new Dictionary<string, string?>
        {
            ["ExternalServices:ProductCatalog:BaseUrl"] = "http://127.0.0.1:1",
            ["InvoiceStorage:ConnectionString"] = string.Empty,
            ["MongoDbSettings:ConnectionString"] = connection,
            ["MongoDbSettings:DatabaseName"] = database,
            ["MongoDbSettings:ShoppingCartsCollectionName"] = "shoppingCarts",
            ["MongoDbSettings:OrdersCollectionName"] = "orders",
            ["MongoDbSettings:ProductVersionsCollectionName"] = "productVersions",
            ["MongoDbSettings:InvoicesCollectionName"] = "invoices",
            ["MongoDbSettings:ClientDataVersionsCollectionName"] = "clientDataVersions"
        }) builder.UseSetting(pair.Key, pair.Value);
        foreach (var pair in overrides ?? []) builder.UseSetting(pair.Key, pair.Value);
    }
    protected override IHost CreateHost(IHostBuilder builder) { var host = base.CreateHost(builder); SuccessfulHosts++; return host; }
}
