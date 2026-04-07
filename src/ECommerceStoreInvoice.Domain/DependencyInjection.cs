using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
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
            services.AddScoped<IValidationPolicy<ClientDataVersion>, ClientDataVersionValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, ClientDataVersionValidationPolicy>();
            services.AddScoped<IValidationPolicy<Order>, OrderValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, OrderValidationPolicy>();
            services.AddScoped<IValidationPolicy<(Order order, OrderStatus newStatus)>, UpdateOrderValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, UpdateOrderValidationPolicy>();
            services.AddScoped<IValidationPolicy<InvoiceOrderStatusValidationContext>, InvoiceValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, InvoiceValidationPolicy>();
            services.AddScoped<IValidationPolicy<ProductVersion>, ProductVersionValidationPolicy>();
            services.AddScoped<IValidationPolicyDescriptorProvider, ProductVersionValidationPolicy>();
            return services;
        }
    }
}
