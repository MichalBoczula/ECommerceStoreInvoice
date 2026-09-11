using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures;

[MemoryDiagnoser]
public class OrderWithProductsMappingBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private OrderWithProductsDocument _document = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = OrderWithProductsDocumentBenchmarkDataFactory.CreateWithLinesAndProductVersions(
            LinesCount,
            Guid.NewGuid(),
            Guid.NewGuid());
    }

    [Benchmark]
    public (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) MapDocumentToDomain()
    {
        return OrderMapping.MapToDomain(_document);
    }
}
