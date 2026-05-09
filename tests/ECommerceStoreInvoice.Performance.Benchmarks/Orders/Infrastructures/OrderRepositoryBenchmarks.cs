using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class OrderRepositoryBenchmarks
    {
        private const string DatabaseName = "order-repository-benchmarks";
        private const string OrdersCollectionName = "orders";

        [Params(1, 10, 100)]
        public int LinesCount { get; set; }

        private MongoDbContainer _mongoContainer = null!;
        private MongoDbContext _context = null!;
        private OrderRepository _repository = null!;

        private readonly List<Guid> _existingOrderIds = new();
        private readonly List<Guid> _existingClientIds = new();
        private readonly List<Order> _existingOrders = new();

        private int _readByIdIterationCounter;
        private int _readByClientIterationCounter;
        private int _updateIterationCounter;

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
                OrdersCollectionName = OrdersCollectionName,
                ShoppingCartsCollectionName = "shopping-carts",
                ProductVersionsCollectionName = "product-versions",
                InvoicesCollectionName = "invoices",
                ClientDataVersionsCollectionName = "client-data-versions"
            };

            _context = new MongoDbContext(Options.Create(settings));
            _repository = new OrderRepository(_context);

            var indexKeys = Builders<OrderDocument>.IndexKeys.Ascending(x => x.ClientId);
            await _context.Orders.Indexes.CreateOneAsync(new CreateIndexModel<OrderDocument>(indexKeys));

            await SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            await _context.Orders.DeleteManyAsync(Builders<OrderDocument>.Filter.Empty);
            _existingOrderIds.Clear();
            _existingClientIds.Clear();
            _existingOrders.Clear();
            _readByIdIterationCounter = 0;
            _readByClientIterationCounter = 0;
            _updateIterationCounter = 0;

            for (var i = 0; i < 200; i++)
            {
                var orderId = Guid.NewGuid();
                var clientId = Guid.NewGuid();
                var doc = OrderDocumentBenchmarkDataFactory.CreateWithLines(LinesCount, orderId, clientId);

                await _context.Orders.InsertOneAsync(doc);

                _existingOrderIds.Add(orderId);
                _existingClientIds.Add(clientId);
                _existingOrders.Add(OrderMapping.MapToDomain(doc));
            }
        }

        [Benchmark]
        public async Task<Order?> GetOrderByOrderId()
        {
            var id = _existingOrderIds[_readByIdIterationCounter % _existingOrderIds.Count];
            _readByIdIterationCounter++;
            return await _repository.GetOrderByOrderId(id);
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<Order>> GetOrdersByClientId()
        {
            var clientId = _existingClientIds[_readByClientIterationCounter % _existingClientIds.Count];
            _readByClientIterationCounter++;
            return await _repository.GetOrdersByClientId(clientId);
        }

        [Benchmark]
        public async Task<Order> CreateOrder()
        {
            var doc = OrderDocumentBenchmarkDataFactory.CreateWithLines(LinesCount, Guid.NewGuid(), Guid.NewGuid());
            return await _repository.CreateOrder(OrderMapping.MapToDomain(doc));
        }

        [Benchmark]
        public async Task<Order> UpdateOrder()
        {
            var order = _existingOrders[_updateIterationCounter % _existingOrders.Count];
            _updateIterationCounter++;
            return await _repository.UpdateOrder(order);
        }

        [GlobalCleanup]
        public async Task Cleanup() => await _mongoContainer.DisposeAsync();
    }
}