using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories
{
    internal sealed class ProductVersionRepository(MongoDbContext context) : IProductVersionRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<ProductVersion> CreateProductVersion(ProductVersion productVersion)
        {
            var productVersionDocument = ProductVersionMapping.MapToDocument(productVersion);

            await _context.ProductVersions.InsertOneAsync(productVersionDocument);

            return productVersion;
        }

        public async Task<ProductVersion?> GetProductVersionById(Guid id)
        {
            var productVersionDocument = await _context.ProductVersions
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (productVersionDocument is null)
                return null;

            return ProductVersionMapping.MapToDomain(productVersionDocument);
        }
    }
}
