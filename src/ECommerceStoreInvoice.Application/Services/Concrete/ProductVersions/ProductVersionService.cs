using Microsoft.Extensions.Logging;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Descriptors.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions
{
    internal sealed class ProductVersionService(
        IProductVersionRepository productVersionRepository,
        IValidationPolicy<ProductVersion> productVersionValidationPolicy,
        IValidationPolicy<Guid> guidValidationPolicy,
        ILogger<ProductVersionService> logger)
        : IProductVersionService
    {
        public async Task<ProductVersionResponseDto> CreateProductVersion(CreateProductVersionRequestDto request)
        {
            logger.LogInformation("Initiating product version creation flow for ProductId: {ProductId}", request.ProductId);

            var descriptor = new CreateProductVersionDescriptor();

            var productVersion = descriptor.MapToDomain(request);
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
    }
}
