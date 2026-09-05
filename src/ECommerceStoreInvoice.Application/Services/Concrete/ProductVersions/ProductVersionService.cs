using Microsoft.Extensions.Logging;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Descriptors.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions
{
    internal sealed class ProductVersionService(
        IProductVersionRepository productVersionRepository,
        IProductServiceClient productServiceClient,
        IValidationPolicy<ProductVersion> productVersionValidationPolicy,
        IValidationPolicy<Guid> guidValidationPolicy,
        IValidationPolicy<IEnumerable<Guid>> guidCollectionValidationPolicy,
        ILogger<ProductVersionService> logger)
        : IProductVersionService
    {
        public async Task<ProductVersionResponseDto> CreateProductVersion(CreateProductVersionRequestDto request)
        {
            logger.LogInformation("Initiating product version creation flow for ProductId: {ProductId}", request.ProductId);

            var descriptor = new CreateProductVersionDescriptor();

            var externalProduct = await descriptor.FetchExternalProduct(request.ProductId, productServiceClient);

            descriptor.ThrowNotFoundExceptionIfExternalProductMissing(request.ProductId, externalProduct);

            var productVersion = descriptor.MapToDomain(externalProduct!);

            var validationResult = await descriptor.Validate(productVersion, productVersionValidationPolicy);
            descriptor.ThrowValidationExceptionIfInvalid(validationResult);

            var createdProductVersion = await descriptor.Save(productVersion, productVersionRepository);

            logger.LogInformation("Successfully created product version. ProductVersionId: {ProductVersionId} for ProductId: {ProductId}", createdProductVersion.Id, createdProductVersion.ProductId);

            return descriptor.MapToResponse(createdProductVersion);
        }

        public async Task<ProductVersionResponseDto> GetProductVersionById(Guid id)
        {
            logger.LogInformation("Processing read request for ProductVersionId: {ProductVersionId}", id);

            var descriptor = new GetProductVersionByIdDescriptor();

            var validationResult = await descriptor.ValidateProductVersionId(id, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfProductVersionIdInvalid(validationResult);

            var productVersion = await descriptor.LoadProductVersion(id, productVersionRepository);
            descriptor.ThrowNotFoundExceptionIfProductVersionMissing(id, productVersion);

            logger.LogInformation("Successfully retrieved product version for ProductVersionId: {ProductVersionId}", id);
            return descriptor.MapToResponse(productVersion!);
        }

        public async Task<IReadOnlyCollection<ProductVersionResponseDto>> CreateMultipleProductVersions(CreateMultipleProductVersionsRequestDto request)
        {
            var descriptor = new CreateMultipleProductVersionsDescriptor();

            var requestValidationResult = await guidCollectionValidationPolicy.Validate(request.ProductIds);
            descriptor.ThrowValidationExceptionIfProductIdsInvalid(requestValidationResult);

            var uniqueProductIds = request.ProductIds.Distinct().ToList();

            logger.LogInformation("Initiating multiple product versions creation flow for {Count} unique products.", uniqueProductIds.Count);

            var externalProducts = await descriptor.FetchExternalProducts(uniqueProductIds, productServiceClient);

            descriptor.ThrowValidationExceptionIfExternalProductsEmpty(externalProducts);

            descriptor.ThrowNotFoundExceptionIfAnyProductMissing(uniqueProductIds, externalProducts);

            var productVersions = descriptor.MapToDomain(externalProducts);

            var validationResults = await descriptor.ValidateAll(productVersions, productVersionValidationPolicy);
            descriptor.ThrowValidationExceptionIfAnyInvalid(validationResults);

            var createdProductVersions = await descriptor.SaveAll(productVersions, productVersionRepository);

            logger.LogInformation("Successfully created {Count} product versions in batch.", createdProductVersions.Count);

            return descriptor.MapToResponse(createdProductVersions);
        }
    }
}
