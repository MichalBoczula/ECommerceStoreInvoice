namespace ECommerceStoreInvoice.Application.Common.FlowDescriptors;

/// <summary>Documents an inline rejection separately from installed validation policies.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class InlineRuleAttribute(string id, string errorName, string condition, string effects) : Attribute
{
    public string Id { get; } = id;
    public string ErrorName { get; } = errorName;
    public string Condition { get; } = condition;
    public string Effects { get; } = effects;
}

/// <summary>A policy invoked in the service immediately outside the descriptor.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class UsesPolicyAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
