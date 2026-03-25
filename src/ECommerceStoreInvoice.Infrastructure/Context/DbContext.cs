using ECommerceStoreInvoice.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Context
{
    internal class DbContext
    {
        private readonly IMongoDatabase _database;

        public DbContext(IOptions<MongoDbSettings> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            _database = client.GetDatabase(options.Value.DatabaseName);
        }
    }
}
