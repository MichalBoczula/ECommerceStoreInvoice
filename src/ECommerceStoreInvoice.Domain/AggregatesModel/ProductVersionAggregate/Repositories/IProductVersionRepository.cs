namespace ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories
{
    public interface IProductVersionRepository
    {
        Task<ProductVersion?> GetProductVersionById(Guid id);
        Task<ProductVersion> CreateProductVersion(ProductVersion productVersion);
        Task<IReadOnlyCollection<ProductVersion>> CreateProductVersions(IReadOnlyCollection<ProductVersion> productVersions);
    }
}
