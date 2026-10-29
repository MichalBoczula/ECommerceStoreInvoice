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

    [Documentation("inv.flow.create-product-version", "Create one product version from a real catalog read; this service flow has no direct public HTTP operation.")]

    internal sealed class CreateProductVersionDescriptor : FlowDescriberBase<CreateProductVersion>
    {
        [FlowStep(order: 1, bpmnId: "FetchExternalProduct", description: "Fetch the requested catalog product through the actual ProductsCatalog adapter.")]
        public async Task<ExternalProductSnapshot?> FetchExternalProduct(Guid productId, IProductServiceClient productServiceClient)
        {
            var externalProducts = await productServiceClient.GetProductsByIds([productId]);
            return externalProducts.FirstOrDefault();
        }

        [FlowStep(order: 2, bpmnId: "IsExternalProductExists", description: "Reject when the requested catalog product does not exist.")]
        public void ThrowNotFoundExceptionIfExternalProductMissing(Guid productId, ExternalProductSnapshot? externalProduct)
        {
            if (externalProduct is null)
            {
                throw new ResourceNotFoundException(nameof(FetchExternalProduct), productId, nameof(ExternalProductSnapshot));
            }
        }

        [FlowStep(order: 3, bpmnId: "MapProductVersionDomain", description: "Construct the create product version domain values without saving them.")]
        public ProductVersion MapToDomain(ExternalProductSnapshot externalProduct)
        {
            return new ProductVersion(
                externalProduct.ProductId,
                externalProduct.Price,
                externalProduct.Name,
                externalProduct.Brand
            );
        }

        [FlowStep(order: 4, bpmnId: "ValidateProductVersion", description: "Apply the registered product-version policy before persistence.")]
        public async Task<ValidationResult> Validate(ProductVersion productVersion, IValidationPolicy<ProductVersion> productVersionValidationPolicy)
        {
            return await productVersionValidationPolicy.Validate(productVersion);
        }

        [FlowStep(order: 5, bpmnId: "IsProductVersionValid", description: "Reject the flow with the complete registered validation errors when invalid.")]
        public void ThrowValidationExceptionIfInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 6, bpmnId: "SaveProductVersion", description: "Persist the newly validated create product version values without rewriting historical versions.")]
        public async Task<ProductVersion> Save(ProductVersion productVersion, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.CreateProductVersion(productVersion);
        }

        [FlowStep(order: 7, bpmnId: "MapProductVersionResponse", description: "Return the complete create product version response from the domain values; this description is not execution tracing.")]
        public ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return MappingConfig.MapToResponse(productVersion);
        }
    }
}