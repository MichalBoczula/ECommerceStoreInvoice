namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    internal sealed class FlowStepAttribute(int order, string bpmnId) : Attribute
    {
        public int Order { get; } = order;
        public string BpmnId { get; } = bpmnId;
    }
}
