using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts
{
    public sealed record ShoppingCartFlowsResponseDto
    {
        public required List<Dictionary<string, FlowDescriptor>> ShoppingCartFlows { get; init; }
    }
}
