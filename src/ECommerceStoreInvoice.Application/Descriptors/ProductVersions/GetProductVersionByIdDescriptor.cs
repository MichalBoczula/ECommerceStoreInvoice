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

    internal sealed class GetProductVersionByIdDescriptor : FlowDescriberBase<GetProductVersionById>
    {
        [FlowStep(order: 1, bpmnId: "ValidateProductVersionId")]
        public async Task<ValidationResult> ValidateProductVersionId(Guid id, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(id);
        }

        [FlowStep(order: 2, bpmnId: "IsProductVersionIdValid")]
        public void ThrowValidationExceptionIfProductVersionIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadProductVersion")]
        public async Task<ProductVersion?> LoadProductVersion(Guid id, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.GetProductVersionById(id);
        }

        [FlowStep(order: 4, bpmnId: "IsProductVersionExists")]
        public void ThrowNotFoundExceptionIfProductVersionMissing(Guid id, ProductVersion? productVersion)
        {
            if (productVersion is null)
            {
                throw new ResourceNotFoundException(nameof(LoadProductVersion), id, nameof(ProductVersion));
            }
        }

        [FlowStep(order: 5, bpmnId: "MapProductVersionResponse")]
        public ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return MappingConfig.MapToResponse(productVersion);
        }
    }
}
