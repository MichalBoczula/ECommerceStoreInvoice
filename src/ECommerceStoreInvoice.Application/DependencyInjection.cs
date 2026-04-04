using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Application.Services.Concrete.Orders;
using ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Concrete.ShoppingCarts;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IProductVersionService, ProductVersionService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IShoppingCartService, ShoppingCartService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderDescriptorService, OrderDescriptorService>();
            services.AddScoped<IShoppingCartDescriptorService, ShoppingCartDescriptorService>();
            services.AddScoped<IProductVersionDescriptorService, ProductVersionDescriptorService>();

            return services;
        }
    }
}
