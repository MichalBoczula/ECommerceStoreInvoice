using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures;

[MemoryDiagnoser]
public class InvoiceMappingBenchmarks
{
    private InvoiceDocument _document = null!;
    private Invoice _domain = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = InvoiceDocumentBenchmarkDataFactory.Create();
        _domain = InvoiceMapping.MapToDomain(_document);
    }

    [Benchmark]
    public Invoice MapDocumentToDomain()
    {
        return InvoiceMapping.MapToDomain(_document);
    }

    [Benchmark]
    public object MapDomainToDocument()
    {
        return InvoiceMapping.MapToDocument(_domain);
    }
}
