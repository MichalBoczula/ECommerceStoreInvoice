namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate
{
    public sealed class Invoice
    {
        public Guid Id { get; init; }
        public Guid OrderId { get; init; }
        public Guid ClientDataVersionId { get; init; }
        public string StorageUrl { get; init; }
        public DateTime CreatedAt { get; init; }

        public Invoice(Guid orderId, Guid clientDataVersionId, string storageUrl)
        {
            Id = Guid.NewGuid();
            OrderId = orderId;
            ClientDataVersionId = clientDataVersionId;
            StorageUrl = storageUrl;
            CreatedAt = DateTime.UtcNow;
        }

        private Invoice(
            Guid id,
            Guid orderId,
            Guid clientDataVersionId,
            string storageUrl,
            DateTime createdAt)
        {
            Id = id;
            OrderId = orderId;
            ClientDataVersionId = clientDataVersionId;
            StorageUrl = storageUrl;
            CreatedAt = createdAt;
        }

        public static Invoice Rehydrate(
            Guid id,
            Guid orderId,
            Guid clientDataVersionId,
            string storageUrl,
            DateTime createdAt)
        {
            return new Invoice(id, orderId, clientDataVersionId, storageUrl, createdAt);
        }
    }
}
