namespace ECommerceStoreInvoice.Domain.Validation.Common;

/// <summary>Authored stable source identity; it does not execute a policy or trace a request.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DocumentationAttribute(string id, string summary) : Attribute
{
    public string Id { get; } = id;
    public string Summary { get; } = summary;
}
