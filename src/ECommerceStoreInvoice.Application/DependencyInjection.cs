using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Concrete;
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

            return services;
        }
    }
}
