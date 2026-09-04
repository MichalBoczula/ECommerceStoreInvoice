using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ProductVersions
{
    internal sealed record CreateProductVersion;

    internal sealed class CreateProductVersionDescriptor : FlowDescriberBase<CreateProductVersion>
    {
        [FlowStep(order: 1, bpmnId: "FetchExternalProduct")]
        public async Task<ExternalProductSnapshot?> FetchExternalProduct(Guid productId, IProductServiceClient productServiceClient)
        {
            var externalProducts = await productServiceClient.GetProductsByIds([productId]);
            return externalProducts.FirstOrDefault();
        }

        [FlowStep(order: 2, bpmnId: "IsExternalProductExists")]
        public void ThrowNotFoundExceptionIfExternalProductMissing(Guid productId, ExternalProductSnapshot? externalProduct)
        {
            if (externalProduct is null)
            {
                throw new ResourceNotFoundException(nameof(FetchExternalProduct), productId, nameof(ExternalProductSnapshot));
            }
        }

        [FlowStep(order: 3, bpmnId: "MapProductVersionDomain")]
        public ProductVersion MapToDomain(ExternalProductSnapshot externalProduct)
        {
            return new ProductVersion(
                externalProduct.ProductId,
                externalProduct.Price,
                externalProduct.Name,
                externalProduct.Brand
            );
        }

        [FlowStep(order: 4, bpmnId: "ValidateProductVersion")]
        public async Task<ValidationResult> Validate(ProductVersion productVersion, IValidationPolicy<ProductVersion> productVersionValidationPolicy)
        {
            return await productVersionValidationPolicy.Validate(productVersion);
        }

        [FlowStep(order: 5, bpmnId: "IsProductVersionValid")]
        public void ThrowValidationExceptionIfInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 6, bpmnId: "SaveProductVersion")]
        public async Task<ProductVersion> Save(ProductVersion productVersion, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.CreateProductVersion(productVersion);
        }

        [FlowStep(order: 7, bpmnId: "MapProductVersionResponse")]
        public ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return MappingConfig.MapToResponse(productVersion);
        }
    }
}