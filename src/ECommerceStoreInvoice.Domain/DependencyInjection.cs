using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDomain(
            this IServiceCollection services)
        {
            services.AddScoped<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>, ShoppingCartLineValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, ShoppingCartLineValidationPolicy>();
            services.AddScoped<IValidationPolicy<Guid>, ClientValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, ClientValidationPolicy>();
            services.AddScoped<IValidationPolicy<Order>, OrderValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, OrderValidationPolicy>();
            services.AddScoped<IValidationPolicy<ProductVersion>, ProductVersionValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, ProductVersionValidationPolicy>();
            return services;
        }
    }
}
