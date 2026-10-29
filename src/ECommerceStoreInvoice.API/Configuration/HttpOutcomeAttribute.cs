namespace ECommerceStoreInvoice.API.Configuration;

/// <summary>A declared outcome family; status and scenario witnesses do not prove an internal branch trace.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HttpOutcomeAttribute(string id, string operationId, int status, string condition, string effects, string failure) : Attribute
{
    public string Id { get; } = id;
    public string OperationId { get; } = operationId;
    public int Status { get; } = status;
    public string Condition { get; } = condition;
    public string Effects { get; } = effects;
    public string Failure { get; } = failure;
}
