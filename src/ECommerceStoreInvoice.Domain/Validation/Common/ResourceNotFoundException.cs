namespace ProductCatalogECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed class ResourceNotFoundException : Exception
    {
        public string ActionName { get; private set; }
        public Guid ResourceId { get; private set; }
        public string ResourceType { get; private set; }

        public ResourceNotFoundException(string actionName, Guid resourceId, string resourceType)
        {
            ActionName = actionName;
            ResourceId = resourceId;
            ResourceType = resourceType;
        }
    }
}