using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class InvoiceRepositoryBenchmarks
    {
        private const string DatabaseName = "invoice-repository-benchmarks";
        private const string InvoicesCollectionName = "invoices";

        private MongoDbContainer _mongoContainer = null!;
        private MongoDbContext _context = null!;
        private InvoiceRepository _repository = null!;

        private readonly List<Guid> _existingInvoiceIds = new();
        private readonly List<Guid> _existingOrderIds = new();

        private int _readByIdIterationCounter;
        private int _readByOrderIterationCounter;

        [GlobalSetup]
        public async Task Setup()
        {
            _mongoContainer = new MongoDbBuilder("mongo:8.0")
                .WithUsername("admin")
                .WithPassword("admin123")
                .WithCreateParameterModifier(parameter =>
                {
                    parameter.HostConfig.Tmpfs = new Dictionary<string, string> { { "/data/db", "rw" } };
                })
                .Build();

            await _mongoContainer.StartAsync();

            var settings = new MongoDbSettings
            {
                ConnectionString = _mongoContainer.GetConnectionString(),
                DatabaseName = DatabaseName,
                InvoicesCollectionName = InvoicesCollectionName,
                ShoppingCartsCollectionName = "shopping-carts",
                OrdersCollectionName = "orders",
                ProductVersionsCollectionName = "product-versions",
                ClientDataVersionsCollectionName = "client-data-versions"
            };

            _context = new MongoDbContext(Options.Create(settings));
            _repository = new InvoiceRepository(_context);

            var indexKeys = Builders<InvoiceDocument>.IndexKeys.Ascending(x => x.OrderId);
            await _context.Invoices.Indexes.CreateOneAsync(new CreateIndexModel<InvoiceDocument>(indexKeys));

            await SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            await _context.Invoices.DeleteManyAsync(Builders<InvoiceDocument>.Filter.Empty);

            _existingInvoiceIds.Clear();
            _existingOrderIds.Clear();
            _readByIdIterationCounter = 0;
            _readByOrderIterationCounter = 0;

            for (var i = 0; i < 200; i++)
            {
                var invoiceId = Guid.NewGuid();
                var orderId = Guid.NewGuid();
                var doc = InvoiceDocumentBenchmarkDataFactory.Create(invoiceId, orderId);

                await _context.Invoices.InsertOneAsync(doc);

                _existingInvoiceIds.Add(invoiceId);
                _existingOrderIds.Add(orderId);
            }
        }

        [Benchmark]
        public async Task<Invoice?> GetInvoiceById()
        {
            var id = _existingInvoiceIds[_readByIdIterationCounter % _existingInvoiceIds.Count];
            _readByIdIterationCounter++;

            return await _repository.GetInvoiceById(id);
        }

        [Benchmark]
        public async Task<Invoice?> GetInvoiceByOrderId()
        {
            var orderId = _existingOrderIds[_readByOrderIterationCounter % _existingOrderIds.Count];
            _readByOrderIterationCounter++;

            return await _repository.GetInvoiceByOrderId(orderId);
        }

        [Benchmark]
        public async Task<Invoice> CreateInvoice()
        {
            var doc = InvoiceDocumentBenchmarkDataFactory.Create(Guid.NewGuid(), Guid.NewGuid());
            var newInvoice = InvoiceMapping.MapToDomain(doc);

            return await _repository.CreateInvoice(newInvoice);
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            if (_mongoContainer is not null)
            {
                await _mongoContainer.DisposeAsync();
            }
        }
    }
}