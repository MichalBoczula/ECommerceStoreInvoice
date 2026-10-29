using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ProductVersions
{
    internal sealed record GetProductVersionById;

    [Documentation("inv.flow.get-product-version-by-id", "Read one stored product version through the internal service; this published descriptor has no direct HTTP operation.")]

    internal sealed class GetProductVersionByIdDescriptor : FlowDescriberBase<GetProductVersionById>
    {
        [FlowStep(order: 1, bpmnId: "ValidateProductVersionId", description: "Validate the stored product-version identifier with the registered GUID policy.")]
        public async Task<ValidationResult> ValidateProductVersionId(Guid id, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(id);
        }

        [FlowStep(order: 2, bpmnId: "IsProductVersionIdValid", description: "Reject the flow with the complete registered validation errors when product version id invalid.")]
        public void ThrowValidationExceptionIfProductVersionIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadProductVersion", description: "Load the requested stored product version without modifying it.")]
        public async Task<ProductVersion?> LoadProductVersion(Guid id, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.GetProductVersionById(id);
        }

        [FlowStep(order: 4, bpmnId: "IsProductVersionExists", description: "Reject when the requested stored product version does not exist.")]
        public void ThrowNotFoundExceptionIfProductVersionMissing(Guid id, ProductVersion? productVersion)
        {
            if (productVersion is null)
            {
                throw new ResourceNotFoundException(nameof(LoadProductVersion), id, nameof(ProductVersion));
            }
        }

        [FlowStep(order: 5, bpmnId: "MapProductVersionResponse", description: "Return the complete get product version by id response from the domain values; this description is not execution tracing.")]
        public ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return MappingConfig.MapToResponse(productVersion);
        }
    }
}
