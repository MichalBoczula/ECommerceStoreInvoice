using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures.Common;

internal static class InvoiceDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    public static InvoiceDocument Create(Guid id, Guid orderId)
    {
        return new InvoiceDocument
        {
            Id = id,
            OrderId = orderId,
            ClientDataVersionId = Guid.NewGuid(),
            StorageUrl = $"https://storage.example/invoices/{id}.pdf",
            CreatedAt = BenchmarkDate
        };
    }

    public static InvoiceDocument Create()
    {
        return Create(Guid.NewGuid(), Guid.NewGuid());
    }
}