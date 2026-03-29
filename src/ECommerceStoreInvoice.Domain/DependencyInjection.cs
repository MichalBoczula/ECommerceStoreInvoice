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
            return services;
        }
    }
}
