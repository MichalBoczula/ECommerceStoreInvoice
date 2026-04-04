using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Configuration
{
    internal sealed class MongoInitializer
    {
        private readonly MongoDbContext _context;

        public MongoInitializer(MongoDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await CreateShoppingCartIndexesAsync(cancellationToken);
            await CreateInvoiceIndexesAsync(cancellationToken);
        }

        private async Task CreateInvoiceIndexesAsync(CancellationToken cancellationToken)
        {
            var orderIdIndex = new CreateIndexModel<InvoiceDocument>(
                Builders<InvoiceDocument>.IndexKeys.Ascending(x => x.OrderId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "UX_Invoice_OrderId"
                });

            await _context.Invoices.Indexes.CreateOneAsync(
                orderIdIndex,
                cancellationToken: cancellationToken);
        }

        private async Task CreateShoppingCartIndexesAsync(CancellationToken cancellationToken)
        {
            var clientIdIndex = new CreateIndexModel<ShoppingCartDocument>(
                Builders<ShoppingCartDocument>.IndexKeys.Ascending(x => x.ClientId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "UX_ShoppingCart_ClientId"
                });

            await _context.ShoppingCarts.Indexes.CreateOneAsync(
                clientIdIndex,
                cancellationToken: cancellationToken);
        }
    }
}
