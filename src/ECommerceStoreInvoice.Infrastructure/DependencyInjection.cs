using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Concret.Products;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDbSettings>(
                configuration.GetSection(MongoDbSettings.SectionName));

            services.AddSingleton<IMongoClient>(provider =>
                new MongoClient(provider.GetRequiredService<IOptions<MongoDbSettings>>().Value.ConnectionString));
            services.AddScoped<MongoDbContext>();
            services.AddScoped<MongoInitializer>();
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
