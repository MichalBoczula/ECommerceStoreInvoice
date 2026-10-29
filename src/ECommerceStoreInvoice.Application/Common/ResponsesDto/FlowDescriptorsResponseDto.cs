using ECommerceStoreInvoice.Application.Common.FlowDescriptors;

namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    /// <summary>Complete source-owned flow documentation; reading it does not execute the described business flows.</summary>
    public sealed record FlowDescriptorsResponseDto
    {
        /// <summary>Ordered array of dictionaries keyed by source descriptor method name.</summary>
        public required List<Dictionary<string, FlowDescriptor>> Flows { get; init; }
    }
}
