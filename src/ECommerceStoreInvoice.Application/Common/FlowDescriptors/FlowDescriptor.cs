namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors
{
    public sealed record FlowDescriptor
    {
        public required string FlowName { get; init; }
        public required List<FlowStepDescriptor> Steps { get; init; }
    }
}
