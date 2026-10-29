using System.Collections.Concurrent;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public partial class ApplicationFactory
{
    internal bool ObserveReadiness { get; set; }
    internal string? TechnicalMongoMode { get; set; }
    internal PdfCancellationProbe? CancellationProbe { get; set; }
    internal string DatabaseName => _database;
    internal string MongoConnection => _connectionString;
    private MongoDbContainer? _technicalMongo;
    private bool _technicalMongoRunning;
    internal ConcurrentQueue<(string Command, string Database)> ReadinessCommands { get; } = new();

    private async Task<bool> StartTechnicalMongoAsync()
    {
        if (TechnicalMongoMode is null) return false;
        var builder = new MongoDbBuilder("mongo:8.0").WithUsername(Username).WithPassword(Password);
        if (TechnicalMongoMode == "replica") builder = builder.WithReplicaSet();
        else if (TechnicalMongoMode != "standalone") throw new InvalidOperationException("Unknown technical Mongo mode.");
        _technicalMongo = builder.Build();
        await _technicalMongo.StartAsync();
        _technicalMongoRunning = true;
        _connectionString = _technicalMongo.GetConnectionString();
        return true;
    }

    private void ConfigureTechnicalServices(IWebHostBuilder builder)
    {
        if (ObserveReadiness)
            builder.ConfigureTestServices(services => services.AddSingleton<IMongoClient>(_ =>
            {
                var settings = MongoClientSettings.FromConnectionString(_connectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
                settings.ConnectTimeout = TimeSpan.FromSeconds(1);
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(500);
                settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
                {
                    if (command.CommandName is "hello" or "ping")
                        ReadinessCommands.Enqueue((command.CommandName, command.DatabaseNamespace.DatabaseName));
                });
                return new MongoClient(settings);
            }));
        if (CancellationProbe is not null)
            builder.ConfigureTestServices(services => Decorate<IInvoicePdfService>(services,
                (inner, _) => new CancellableInvoicePdfService(inner, CancellationProbe)));
    }

    internal async Task StopTechnicalMongoAsync()
    {
        if (_technicalMongo is null) throw new InvalidOperationException("Stopping shared Mongo is prohibited.");
        await _technicalMongo.StopAsync();
        _technicalMongoRunning = false;
    }

    internal async Task RestartTechnicalMongoAsync()
    {
        if (_technicalMongo is null) throw new InvalidOperationException("Restarting shared Mongo is prohibited.");
        await _technicalMongo.StartAsync();
        _technicalMongoRunning = true;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await Services.GetRequiredService<IMongoClient>().GetDatabase(_database)
            .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: deadline.Token);
    }

    internal Task<BsonDocument> TechnicalHelloAsync() => new MongoClient(_connectionString).GetDatabase("admin")
        .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));

    internal async Task<Dictionary<string, BsonDocument[]>> TechnicalIndexesAsync()
    {
        var database = new MongoClient(_connectionString).GetDatabase(_database);
        var result = new Dictionary<string, BsonDocument[]>();
        foreach (var name in new[] { "shoppingCarts", "orders", "productVersions", "invoices", "clientDataVersions" })
        {
            using var cursor = await database.GetCollection<BsonDocument>(name).Indexes.ListAsync();
            result[name] = (await cursor.ToListAsync()).OrderBy(d => d["name"].AsString).ToArray();
        }
        return result;
    }

    internal async Task<bool> DatabaseExistsAsync(string database)
    {
        using var cursor = await new MongoClient(_connectionString).ListDatabaseNamesAsync();
        return (await cursor.ToListAsync()).Contains(database, StringComparer.Ordinal);
    }

    internal async Task ReplaceInvoiceStorageFixtureAsync(Guid invoiceId, string url)
    {
        var collection = new MongoClient(_connectionString).GetDatabase(_database).GetCollection<BsonDocument>("invoices");
        var result = await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(invoiceId, GuidRepresentation.Standard)),
            Builders<BsonDocument>.Update.Set("StorageUrl", url));
        if (result.ModifiedCount != 1) throw new InvalidOperationException("Expected exactly one invoice storage fixture update.");
    }
}
