using ECommerceStoreInvoice.Infrastructure.Context;

namespace ECommerceStoreInvoice.Infrastructure.Configuration
{
    internal class MongoInitializer
    {
        private readonly DbContext _context;

        public MongoInitializer(DbContext context)
        {
            _context = context;
        }

        public async Task EnsureIndexesCreatedAsync()
        {
        }
    }
}
