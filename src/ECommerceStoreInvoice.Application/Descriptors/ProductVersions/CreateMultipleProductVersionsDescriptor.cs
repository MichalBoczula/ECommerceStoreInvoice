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
    internal sealed record CreateMultipleProductVersions;

    internal sealed class CreateMultipleProductVersionsDescriptor : FlowDescriberBase<CreateMultipleProductVersions>
    {
        [FlowStep(order: 1, bpmnId: "FetchMultipleExternalProducts")]
        public async Task<IReadOnlyCollection<ExternalProductSnapshot>> FetchExternalProducts(IEnumerable<Guid> productIds, IProductServiceClient productServiceClient)
        {
            return await productServiceClient.GetProductsByIds(productIds);
        }

        [FlowStep(order: 2, bpmnId: "EnsureAllExternalProductsExist")]
        public void ThrowNotFoundExceptionIfAnyProductMissing(IEnumerable<Guid> requestedIds, IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            var foundIds = externalProducts.Select(p => p.ProductId).ToHashSet();
            var missingId = requestedIds.FirstOrDefault(id => !foundIds.Contains(id));

            if (missingId != Guid.Empty)
            {
                throw new ResourceNotFoundException(nameof(FetchExternalProducts), missingId, nameof(ExternalProductSnapshot));
            }
        }

        [FlowStep(order: 3, bpmnId: "MapMultipleProductVersionsDomain")]
        public IReadOnlyCollection<ProductVersion> MapToDomain(IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            return externalProducts.Select(ep => new ProductVersion(
                ep.ProductId,
                ep.Price,
                ep.Name,
                ep.Brand
            )).ToList();
        }

        [FlowStep(order: 4, bpmnId: "ValidateMultipleProductVersions")]
        public async Task<IReadOnlyCollection<ValidationResult>> ValidateAll(IReadOnlyCollection<ProductVersion> productVersions, IValidationPolicy<ProductVersion> productVersionValidationPolicy)
        {
            var results = new List<ValidationResult>();
            foreach (var pv in productVersions)
            {
                results.Add(await productVersionValidationPolicy.Validate(pv));
            }
            return results;
        }

        [FlowStep(order: 5, bpmnId: "AreAllProductVersionsValid")]
        public void ThrowValidationExceptionIfAnyInvalid(IReadOnlyCollection<ValidationResult> validationResults)
        {
            var firstInvalidResult = validationResults.FirstOrDefault(r => !r.IsValid);
            if (firstInvalidResult != null)
            {
                throw new ValidationException(firstInvalidResult);
            }
        }

        [FlowStep(order: 6, bpmnId: "SaveMultipleProductVersions")]
        public async Task<IReadOnlyCollection<ProductVersion>> SaveAll(IReadOnlyCollection<ProductVersion> productVersions, IProductVersionRepository productVersionRepository)
        {
            var savedVersions = new List<ProductVersion>();
            foreach (var pv in productVersions)
            {
                savedVersions.Add(await productVersionRepository.CreateProductVersion(pv));
            }
            return savedVersions;
        }

        [FlowStep(order: 7, bpmnId: "MapMultipleProductVersionsResponse")]
        public IReadOnlyCollection<ProductVersionResponseDto> MapToResponse(IReadOnlyCollection<ProductVersion> productVersions)
        {
            return productVersions.Select(MappingConfig.MapToResponse).ToList();
        }
    }
}