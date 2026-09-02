namespace ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices
{
    public interface IProductServiceClient
    {
        Task<ExternalProductSnapshot?> GetProductById(Guid productId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ExternalProductSnapshot>> GetProductsByIds(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
    }
}
