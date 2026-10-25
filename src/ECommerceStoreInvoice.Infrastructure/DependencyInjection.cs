using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Concret.Products;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Health;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Azure.Storage.Blobs;
using ECommerceStoreInvoice.Infrastructure.Storage;

namespace ECommerceStoreInvoice.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var storageConnection = configuration["InvoiceStorage:ConnectionString"];
            if (!string.IsNullOrWhiteSpace(storageConnection))
            {
                var containerName = configuration["InvoiceStorage:ContainerName"];
                if (string.IsNullOrWhiteSpace(containerName))
                    throw new InvalidOperationException("InvoiceStorage:ContainerName must be configured with Blob storage.");
                services.AddSingleton<IInvoiceDocumentStorage>(
                    new BlobInvoiceDocumentStorage(new BlobContainerClient(storageConnection, containerName)));
            }

            services.Configure<MongoDbSettings>(
                configuration.GetSection(MongoDbSettings.SectionName));

            services.AddSingleton<IMongoClient>(provider =>
            {
                var connectionString = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value.ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString) ||
                    !(connectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
                      connectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException(
                        "MongoDbSettings:ConnectionString must be configured with a mongodb:// or mongodb+srv:// URI.");

                return new MongoClient(connectionString);
            });
            services.AddScoped<MongoDbContext>();
            services.AddScoped<MongoInitializer>();
            services.AddHealthChecks().AddCheck<MongoReadinessHealthCheck>(
                "mongo-replica-set",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5));
            services.AddScoped<IOrderWriteTransaction, MongoOrderWriteTransaction>();

            services.AddScoped<IProductVersionRepository, ProductVersionRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceGenerationRepository, InvoiceGenerationRepository>();
            services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IClientDataVersionRepository, ClientDataVersionRepository>();

            services.AddScoped<IProductServiceClient, ExternalProductServiceClient>();

            return services;
        }
    }
}
