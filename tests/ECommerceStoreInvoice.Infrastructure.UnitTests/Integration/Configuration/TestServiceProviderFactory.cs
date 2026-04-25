using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration
{
    internal static class TestServiceProviderFactory
    {
        public static ServiceProvider Create(string mongoConnectionString, string databaseName)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MongoDbSettings:ConnectionString"] = mongoConnectionString,
                    ["MongoDbSettings:DatabaseName"] = databaseName,
                    ["MongoDbSettings:ShoppingCartsCollectionName"] = "shopping-carts",
                    ["MongoDbSettings:OrdersCollectionName"] = "orders",
                    ["MongoDbSettings:ProductVersionsCollectionName"] = "product-versions",
                    ["MongoDbSettings:InvoicesCollectionName"] = "invoices",
                    ["MongoDbSettings:ClientDataVersionsCollectionName"] = "client-data-versions"
                })
                .Build();

            var services = new ServiceCollection();

            services.AddInfrastructure(configuration);

            return services.BuildServiceProvider();
        }
    }
}
