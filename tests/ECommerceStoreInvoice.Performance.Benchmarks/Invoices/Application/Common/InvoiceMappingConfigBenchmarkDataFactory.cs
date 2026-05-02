using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common;

internal static class InvoiceMappingConfigBenchmarkDataFactory
{
    private static readonly DateTime BenchmarkDate = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    public static Invoice CreateDomainInvoice()
    {
        return Invoice.Rehydrate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "https://storage.example/invoices/42.pdf",
            BenchmarkDate);
    }
}
