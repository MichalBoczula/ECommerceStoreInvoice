using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Descriptors.ProductVersions
{
    internal sealed record GetProductVersionById;

    internal sealed class GetProductVersionByIdDescriptor : FlowDescriberBase<GetProductVersionById>
    {
        [FlowStep(order: 1, bpmnId: "LoadProductVersion")]
        public async Task<ProductVersion?> LoadProductVersion(Guid id, IProductVersionRepository productVersionRepository)
        {
            return await productVersionRepository.GetProductVersionById(id);
        }

        [FlowStep(order: 2, bpmnId: "MapProductVersionResponse")]
        public ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return MappingConfig.MapToResponse(productVersion);
        }
    }
}
