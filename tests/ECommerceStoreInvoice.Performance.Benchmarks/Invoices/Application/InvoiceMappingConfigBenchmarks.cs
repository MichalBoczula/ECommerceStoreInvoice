using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application;

[MemoryDiagnoser]
public class InvoiceMappingConfigBenchmarks
{
    private Invoice _invoice = null!;

    [GlobalSetup]
    public void Setup()
    {
        _invoice = InvoiceMappingConfigBenchmarkDataFactory.CreateDomainInvoice();
    }

    [Benchmark]
    public InvoiceResponseDto MapInvoiceToResponse()
    {
        return MappingConfig.MapToResponse(_invoice);
    }
}
