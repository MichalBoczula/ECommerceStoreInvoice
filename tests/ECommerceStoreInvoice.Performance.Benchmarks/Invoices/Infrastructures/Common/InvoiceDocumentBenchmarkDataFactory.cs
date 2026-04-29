using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures.Common;

internal static class InvoiceDocumentBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    public static InvoiceDocument Create()
    {
        return new InvoiceDocument
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            OrderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ClientDataVersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            StorageUrl = "https://storage.example/invoices/42.pdf",
            CreatedAt = BenchmarkDate
        };
    }
}
