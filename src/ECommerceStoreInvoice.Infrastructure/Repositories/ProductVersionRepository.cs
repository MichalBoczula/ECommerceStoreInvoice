using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
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

        public async Task<IReadOnlyCollection<ProductVersion>> CreateProductVersions(IReadOnlyCollection<ProductVersion> productVersions)
        {
            if (productVersions.Count == 0)
                return Array.Empty<ProductVersion>();

            var documents = productVersions.Select(ProductVersionMapping.MapToDocument).ToList();

            await _context.ProductVersions.InsertManyAsync(documents);

            return productVersions;
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

        public async Task<IReadOnlyCollection<ProductVersion>> GetProductVersionsByIds(IReadOnlyCollection<Guid> ids)
        {
            if (ids.Count == 0)
                return Array.Empty<ProductVersion>();

            var distinctIds = ids.Distinct().ToList();

            var filter = Builders<ProductVersionDocument>.Filter.In(x => x.Id, distinctIds);

            var documents = await _context.ProductVersions
                .Find(filter)
                .ToListAsync();

            return documents
                .Select(ProductVersionMapping.MapToDomain)
                .ToList();
        }
    }
}