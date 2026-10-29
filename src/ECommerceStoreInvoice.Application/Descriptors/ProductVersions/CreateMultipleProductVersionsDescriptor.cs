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

    [Documentation("inv.flow.create-multiple-product-versions", "Validate IDs, fetch catalog products and save product versions through the internal bulk service; this descriptor is neither published nor a direct HTTP entry.")]

    [UsesPolicy("GuidCollectionValidationPolicy")]
    internal sealed class CreateMultipleProductVersionsDescriptor : FlowDescriberBase<CreateMultipleProductVersions>
    {
        [FlowStep(order: 1, bpmnId: "ValidateProductIds", description: "Reject the bulk request when the externally invoked ID-collection policy reports errors.")]
        public void ThrowValidationExceptionIfProductIdsInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult);
        }

        [FlowStep(order: 2, bpmnId: "FetchMultipleExternalProducts", description: "Fetch all distinct requested catalog products in one adapter call.")]
        public async Task<IReadOnlyCollection<ExternalProductSnapshot>> FetchExternalProducts(IEnumerable<Guid> productIds, IProductServiceClient productServiceClient)
        {
            return await productServiceClient.GetProductsByIds(productIds);
        }

        [FlowStep(order: 3, bpmnId: "ValidateExternalProductsNotEmpty", description: "Reject an empty external product list through the inline ExternalProductsList error.")]
        [InlineRule("inv.rule.external-products-list", "ExternalProductsList", "The internal bulk catalog call returns no products.", "Reject before mapping or saving product versions.")]
        public void ThrowValidationExceptionIfExternalProductsEmpty(IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            if (externalProducts is null || !externalProducts.Any())
            {
                var result = new ValidationResult();
                result.AddValidationError(new ValidationError
                {
                    Entity = nameof(ExternalProductSnapshot),
                    Name = "ExternalProductsList",
                    Message = "The external service returned an empty list of products."
                });

                throw new ValidationException(result);
            }
        }

        [FlowStep(order: 4, bpmnId: "EnsureAllExternalProductsExist", description: "Reject if any distinct requested product is absent from the fetched catalog result.")]
        public void ThrowNotFoundExceptionIfAnyProductMissing(IEnumerable<Guid> requestedIds, IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            var foundIds = externalProducts.Select(p => p.ProductId).ToHashSet();
            var missingId = requestedIds.FirstOrDefault(id => !foundIds.Contains(id));

            if (missingId != Guid.Empty)
            {
                throw new ResourceNotFoundException(nameof(FetchExternalProducts), missingId, nameof(ExternalProductSnapshot));
            }
        }

        [FlowStep(order: 5, bpmnId: "MapMultipleProductVersionsDomain", description: "Construct the create multiple product versions domain values without saving them.")]
        public IReadOnlyCollection<ProductVersion> MapToDomain(IReadOnlyCollection<ExternalProductSnapshot> externalProducts)
        {
            return externalProducts.Select(ep => new ProductVersion(
                ep.ProductId,
                ep.Price,
                ep.Name,
                ep.Brand
            )).ToList();
        }

        [FlowStep(order: 6, bpmnId: "ValidateMultipleProductVersions", description: "Apply the registered product-version policy independently to all constructed versions.")]
        public async Task<IReadOnlyCollection<ValidationResult>> ValidateAll(IReadOnlyCollection<ProductVersion> productVersions, IValidationPolicy<ProductVersion> productVersionValidationPolicy)
        {
            var results = new List<ValidationResult>();
            foreach (var pv in productVersions)
            {
                results.Add(await productVersionValidationPolicy.Validate(pv));
            }
            return results;
        }

        [FlowStep(order: 7, bpmnId: "AreAllProductVersionsValid", description: "Reject the complete invalid product-version result before saving any version.")]
        public void ThrowValidationExceptionIfAnyInvalid(IReadOnlyCollection<ValidationResult> validationResults)
        {
            var firstInvalidResult = validationResults.FirstOrDefault(r => !r.IsValid);
            if (firstInvalidResult != null)
            {
                throw new ValidationException(firstInvalidResult);
            }
        }

        [FlowStep(order: 8, bpmnId: "SaveMultipleProductVersions", description: "Persist the newly constructed product versions and return the stored collection.")]
        public async Task<IReadOnlyCollection<ProductVersion>> SaveAll(IReadOnlyCollection<ProductVersion> productVersions, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.CreateProductVersions(productVersions);
        }

        [FlowStep(order: 9, bpmnId: "MapMultipleProductVersionsResponse", description: "Return the complete create multiple product versions response from the domain values; this description is not execution tracing.")]
        public IReadOnlyCollection<ProductVersionResponseDto> MapToResponse(IReadOnlyCollection<ProductVersion> productVersions)
        {
            return productVersions.Select(MappingConfig.MapToResponse).ToList();
        }
    }
}
