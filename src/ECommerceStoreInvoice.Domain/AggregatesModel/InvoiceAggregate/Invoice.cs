using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate
{
    public sealed class Invoice
    {
        public Guid Id { get; private set; }
        public OrderSnapshot Order { get; private set; }
        public ClientSnapshot Client { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string InvoiceNumber { get; private set; }

        public Invoice(OrderSnapshot order, ClientSnapshot client)
        {
            Id = Guid.NewGuid();
            Order = order;
            Client = client;
            CreatedAt = DateTime.UtcNow;
            InvoiceNumber = GenerateInvoiceNumber();
        }

        private string GenerateInvoiceNumber()
        {
            throw new NotImplementedException();
        }
    }
}
