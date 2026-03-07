using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate
{
    public sealed class Invoice
    {
        public Guid Id { get; init; }
        public OrderSnapshot Order { get; init; }
        public ClientSnapshot Client { get; init; }
        public DateTime CreatedAt { get; init; }
        public string InvoiceNumber { get; init; }
        public bool PdfGenerated { get; private set; }
        public string? PdfUrl { get; private set; }

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

        private void GeneratePdf()
        {
            PdfGenerated = true;
            PdfUrl = $"https://example.com/invoices/{Id}.pdf";
        }
    }
}
