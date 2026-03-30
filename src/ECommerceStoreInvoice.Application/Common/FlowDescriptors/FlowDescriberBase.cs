using System.Reflection;

namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors
{
    internal abstract class FlowDescriberBase<TFlow>
    {
        public FlowDescriptor Describe()
        {
            var steps = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(method => new
                {
                    Method = method,
                    Attribute = method.GetCustomAttribute<FlowStepAttribute>()
                })
                .Where(x => x.Attribute is not null)
                .OrderBy(x => x.Attribute!.Order)
                .Select(x => new FlowStepDescriptor
                {
                    Order = x.Attribute!.Order,
                    StepName = x.Method.Name,
                    BpmnId = x.Attribute.BpmnId
                })
                .ToList();

            return new FlowDescriptor
            {
                FlowName = typeof(TFlow).Name,
                Steps = steps
            };
        }
    }
}
