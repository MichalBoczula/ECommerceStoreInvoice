using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            return services;
        }
    }
}
