using Testcontainers.MongoDb;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration
{
    public sealed class MongoDbTestFixture : IAsyncLifetime
    {
        private readonly MongoDbContainer _mongoDbContainer =
            new MongoDbBuilder("mongo:8.0")
                .Build();

        public string ConnectionString => _mongoDbContainer.GetConnectionString();

        public Task InitializeAsync()
        {
            return _mongoDbContainer.StartAsync();
        }

        public Task DisposeAsync()
        {
            return _mongoDbContainer.DisposeAsync().AsTask();
        }
    }
}
