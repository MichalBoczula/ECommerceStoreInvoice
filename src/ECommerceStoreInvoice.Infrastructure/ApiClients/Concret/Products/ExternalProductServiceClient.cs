using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using Microsoft.Kiota.Abstractions;

namespace ECommerceStoreInvoice.Infrastructure.ApiClients.Concret.Products
{
    internal sealed class ExternalProductServiceClient : IProductServiceClient
    {
        private readonly ProductApiClient _kiotaClient;

        public ExternalProductServiceClient(ProductApiClient kiotaClient)
        {
            _kiotaClient = kiotaClient;
        }

        public async Task<IReadOnlyCollection<ExternalProductSnapshot>> GetProductsByIds(
            IEnumerable<Guid> productIds,
            CancellationToken cancellationToken = default)
        {
            var idsList = productIds.Distinct().Cast<Guid?>().ToList();
            if (idsList.Count == 0)
            {
                return Array.Empty<ExternalProductSnapshot>();
            }

            try
            {
                var response = await _kiotaClient.MobilePhones.ByIds.PostAsync(
                    body: idsList,
                    cancellationToken: cancellationToken);

                if (response is null || response.Count == 0)
                {
                    return Array.Empty<ExternalProductSnapshot>();
                }

                return response
                    .Where(dto => dto is not null)
                    .Select(ProductVersionMapping.MapToSnapshot)
                    .ToList();
            }
            catch (ApiException ex) when (ex.ResponseStatusCode == 404)
            {
                return Array.Empty<ExternalProductSnapshot>();
            }
        }
    }
}