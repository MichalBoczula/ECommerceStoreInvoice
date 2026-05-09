using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ClientDataVersionRepositoryBenchmarks
    {
        private const string DatabaseName = "client-version-repository-benchmarks";
        private const string ClientDataVersionsCollectionName = "client-data-versions";

        private MongoDbContainer _mongoContainer = null!;
        private MongoDbContext _context = null!;
        private ClientDataVersionRepository _repository = null!;

        private readonly List<Guid> _existingClientIds = new();
        private int _readIterationCounter;

        [GlobalSetup]
        public async Task Setup()
        {
            _mongoContainer = new MongoDbBuilder("mongo:8.0")
                .WithUsername("admin")
                .WithPassword("admin123")
                .WithCreateParameterModifier(p => p.HostConfig.Tmpfs = new Dictionary<string, string> { { "/data/db", "rw" } })
                .Build();

            await _mongoContainer.StartAsync();

            var settings = new MongoDbSettings
            {
                ConnectionString = _mongoContainer.GetConnectionString(),
                DatabaseName = DatabaseName,
                ClientDataVersionsCollectionName = ClientDataVersionsCollectionName,
                ShoppingCartsCollectionName = "shopping-carts",
                OrdersCollectionName = "orders",
                ProductVersionsCollectionName = "product-versions",
                InvoicesCollectionName = "invoices"
            };

            _context = new MongoDbContext(Options.Create(settings));
            _repository = new ClientDataVersionRepository(_context);

            var indexKeys = Builders<ClientDataVersionDocument>.IndexKeys
                .Ascending(x => x.ClientId)
                .Descending(x => x.CreatedAt);

            await _context.ClientDataVersions.Indexes.CreateOneAsync(new CreateIndexModel<ClientDataVersionDocument>(indexKeys));

            await SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            await _context.ClientDataVersions.DeleteManyAsync(Builders<ClientDataVersionDocument>.Filter.Empty);
            _existingClientIds.Clear();
            _readIterationCounter = 0;

            for (var i = 0; i < 200; i++)
            {
                var clientId = Guid.NewGuid();
                var v1 = ClientDataVersionDocumentBenchmarkDataFactory.Create(Guid.NewGuid(), clientId, DateTime.UtcNow.AddDays(-1));
                var v2 = ClientDataVersionDocumentBenchmarkDataFactory.Create(Guid.NewGuid(), clientId, DateTime.UtcNow);

                await _context.ClientDataVersions.InsertManyAsync(new[] { v1, v2 });
                _existingClientIds.Add(clientId);
            }
        }

        [Benchmark]
        public async Task<ClientDataVersion?> GetByClientId()
        {
            var clientId = _existingClientIds[_readIterationCounter % _existingClientIds.Count];
            _readIterationCounter++;
            return await _repository.GetByClientId(clientId);
        }

        [Benchmark]
        public async Task Create()
        {
            var doc = ClientDataVersionDocumentBenchmarkDataFactory.Create();
            await _repository.Create(ClientDataVersionMapping.MapToDomain(doc));
        }

        [GlobalCleanup]
        public async Task Cleanup() => await _mongoContainer.DisposeAsync();
    }
}
