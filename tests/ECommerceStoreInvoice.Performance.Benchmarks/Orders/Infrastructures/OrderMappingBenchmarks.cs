using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures;

[MemoryDiagnoser]
public class OrderMappingBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private OrderDocument _document = null!;
    private Order _domain = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = (OrderDocument)OrderDocumentBenchmarkDataFactory.CreateWithLines(LinesCount);
        _domain = OrderMapping.MapToDomain(_document);
    }

    [Benchmark]
    public Order MapDocumentToDomain()
    {
        return OrderMapping.MapToDomain(_document);
    }

    [Benchmark]
    public object MapDomainToDocument()
    {
        return OrderMapping.MapToDocument(_domain);
    }
}
