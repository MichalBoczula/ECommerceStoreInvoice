namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors
{
    /// <summary>Source-authored business flow descriptor.</summary>
    public sealed record FlowDescriptor
    {
        /// <summary>Stable authored flow name.</summary>
        public required string FlowName { get; init; }
        /// <summary>Ordered flow steps, each with its authored identity.</summary>
        public required List<FlowStepDescriptor> Steps { get; init; }
    }
}
