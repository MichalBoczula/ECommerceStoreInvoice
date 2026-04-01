using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal sealed class ProductVersionService(
        IProductVersionRepository productVersionRepository)
        : IProductVersionService
    {
        public async Task<ProductVersionResponseDto> CreateProductVersion(CreateProductVersionRequestDto request)
        {
            var productVersion = new ProductVersion(
                request.ProductId,
                new Money(request.PriceAmount, request.PriceCurrency),
                request.Name,
                request.Brand);

            var createdProductVersion = await productVersionRepository.CreateProductVersion(productVersion);

            return MappingConfig.MapToResponse(createdProductVersion);
        }

        public async Task<ProductVersionResponseDto?> GetProductVersionById(Guid id)
        {
            var productVersion = await productVersionRepository.GetProductVersionById(id);

            if (productVersion is null)
                return null;

            return MappingConfig.MapToResponse(productVersion);
        }
    }
}
