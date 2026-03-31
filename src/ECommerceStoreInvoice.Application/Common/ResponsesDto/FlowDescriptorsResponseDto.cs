using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    public sealed record FlowDescriptorsResponseDto
    {
        public required List<Dictionary<string, FlowDescriptor>> Flows { get; init; }
    }
}
