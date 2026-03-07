using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDomain(
            this IServiceCollection services)
        {
            //services.AddScoped<IValidationPolicy<T>, T>();
            //services.AddScoped<IValidationPolicyDescriptorProvider, T>();
            return services;
        }
    }
}
