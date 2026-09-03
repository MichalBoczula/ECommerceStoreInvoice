namespace ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices
{
    public interface IProductServiceClient
    {
        Task<IReadOnlyCollection<ExternalProductSnapshot>> GetProductsByIds(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
    }
}
