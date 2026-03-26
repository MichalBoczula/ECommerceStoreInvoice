using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Infrastructure.Configuration
{
    public static class ApplicationInitializationExtensions
    {
        public static async Task InitializeInfrastructureAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var mongoInitializer = scope.ServiceProvider.GetRequiredService<MongoInitializer>();
            await mongoInitializer.InitializeAsync();
        }
    }
}
