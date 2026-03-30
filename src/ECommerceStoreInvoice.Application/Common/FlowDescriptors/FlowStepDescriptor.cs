namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors
{
    public sealed record FlowStepDescriptor
    {
        public required int Order { get; init; }
        public required string StepName { get; init; }
        public required string BpmnId { get; init; }
    }
}
