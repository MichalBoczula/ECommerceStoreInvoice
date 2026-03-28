namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public class ResourceAlreadyExistsException : Exception
    {
        public string ActionName { get; private set; }
        public Guid ResourceId { get; private set; }
        public string ResourceType { get; private set; }

        public ResourceAlreadyExistsException(string actionName, Guid resourceId, string resourceType)
        {
            ActionName = actionName;
            ResourceId = resourceId;
            ResourceType = resourceType;
        }
    }
}
