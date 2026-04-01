using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ProductVersions
{
    internal sealed record CreateProductVersion;

    internal sealed class CreateProductVersionDescriptor : FlowDescriberBase<CreateProductVersion>
    {
        [FlowStep(order: 1, bpmnId: "MapProductVersionDomain")]
        public ProductVersion MapToDomain(CreateProductVersionRequestDto request)
        {
            return new ProductVersion(
                request.ProductId,
                new Money(request.PriceAmount, request.PriceCurrency),
                request.Name,
                request.Brand);
        }

        [FlowStep(order: 2, bpmnId: "ValidateProductVersion")]
        public async Task<ValidationResult> Validate(ProductVersion productVersion, IValidationPolicy<ProductVersion> productVersionValidationPolicy)
        {
            return await productVersionValidationPolicy.Validate(productVersion);
        }

        [FlowStep(order: 3, bpmnId: "IsProductVersionValid")]
        public void ThrowValidationExceptionIfInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 4, bpmnId: "SaveProductVersion")]
        public async Task<ProductVersion> Save(ProductVersion productVersion, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.CreateProductVersion(productVersion);
        }

        [FlowStep(order: 5, bpmnId: "MapProductVersionResponse")]
        public ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return MappingConfig.MapToResponse(productVersion);
        }
    }
}
