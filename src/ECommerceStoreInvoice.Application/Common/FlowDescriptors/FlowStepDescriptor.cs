namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors
{
    /// <summary>One source-authored flow step.</summary>
    public sealed record FlowStepDescriptor
    {
        /// <summary>Positive int32 ordering value within the flow.</summary>
        public required int Order { get; init; }
        /// <summary>Readable authored step name.</summary>
        public required string StepName { get; init; }
        /// <summary>Authored BPMN step identity; this is documentation metadata rather than a runtime trace.</summary>
        public required string BpmnId { get; init; }
    }
}
