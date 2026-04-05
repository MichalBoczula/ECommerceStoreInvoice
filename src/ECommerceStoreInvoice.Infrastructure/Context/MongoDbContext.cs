using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Context
{
    internal sealed class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        public MongoDbContext(IOptions<MongoDbSettings> options)
        {
            _settings = options.Value;

            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
                throw new InvalidOperationException("MongoDbSettings.ConnectionString is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.DatabaseName))
                throw new InvalidOperationException("MongoDbSettings.DatabaseName is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.ShoppingCartsCollectionName))
                throw new InvalidOperationException("MongoDbSettings.ShoppingCartsCollectionName is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.OrdersCollectionName))
                throw new InvalidOperationException("MongoDbSettings.OrdersCollectionName is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.ProductVersionsCollectionName))
                throw new InvalidOperationException("MongoDbSettings.ProductVersionsCollectionName is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.InvoicesCollectionName))
                throw new InvalidOperationException("MongoDbSettings.InvoicesCollectionName is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.ClientDataVersionsCollectionName))
                throw new InvalidOperationException("MongoDbSettings.ClientDataVersionsCollectionName is not configured.");

            var client = new MongoClient(_settings.ConnectionString);
            _database = client.GetDatabase(_settings.DatabaseName);
        }

        public IMongoCollection<ShoppingCartDocument> ShoppingCarts =>
            _database.GetCollection<ShoppingCartDocument>(_settings.ShoppingCartsCollectionName);

        public IMongoCollection<OrderDocument> Orders =>
            _database.GetCollection<OrderDocument>(_settings.OrdersCollectionName);

        public IMongoCollection<ProductVersionDocument> ProductVersions =>
            _database.GetCollection<ProductVersionDocument>(_settings.ProductVersionsCollectionName);

        public IMongoCollection<InvoiceDocument> Invoices =>
            _database.GetCollection<InvoiceDocument>(_settings.InvoicesCollectionName);

        public IMongoCollection<ClientDataVersionDocument> ClientDataVersions =>
            _database.GetCollection<ClientDataVersionDocument>(_settings.ClientDataVersionsCollectionName);
    }
}
