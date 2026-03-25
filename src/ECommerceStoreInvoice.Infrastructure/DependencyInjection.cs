using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IProductVersionRepository, ProductVersionRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<DbContext>();
            services.AddScoped<MongoInitializer>();

            return services;
        }
    }
}
