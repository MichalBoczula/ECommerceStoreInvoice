using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;

namespace ECommerceStoreInvoice.Infrastructure.Mapping
{
    internal static class ProductVersionMapping
    {
        public static ProductVersionDocument MapToDocument(ProductVersion productVersion)
        {
            return new ProductVersionDocument
            {
                Id = productVersion.Id,
                IsActive = productVersion.IsActive,
                CreatedAt = productVersion.CreatedAt,
                DeactivatedAt = productVersion.DeactivatedAt,
                ProductId = productVersion.ProductId,
                PriceAmount = productVersion.Price.Amount,
                PriceCurrency = productVersion.Price.Currency,
                Name = productVersion.Name,
                Brand = productVersion.Brand
            };
        }

        public static ProductVersion MapToDomain(ProductVersionDocument productVersionDocument)
        {
            return ProductVersion.Rehydrate(
                productVersionDocument.Id,
                productVersionDocument.IsActive,
                productVersionDocument.CreatedAt,
                productVersionDocument.DeactivatedAt,
                productVersionDocument.ProductId,
                new Money(productVersionDocument.PriceAmount, productVersionDocument.PriceCurrency),
                productVersionDocument.Name,
                productVersionDocument.Brand);
        }
    }
}
