using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures;

[MemoryDiagnoser]
public class ShoppingCartMappingBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private ShoppingCartDocument _document = null!;
    private ShoppingCart _domain = null!;
    private ShoppingCartLine _line = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = ShoppingCartDocumentBenchmarkDataFactory.CreateWithLines(LinesCount);
        _domain = ShoppingCartMapping.MapToDomain(_document);
        _line = _domain.Lines.First();
    }

    [Benchmark]
    public ShoppingCart MapDocumentToDomain()
    {
        return ShoppingCartMapping.MapToDomain(_document);
    }

    [Benchmark]
    public object MapDomainToDocument()
    {
        return ShoppingCartMapping.MapToDocument(_domain);
    }

    [Benchmark]
    public object MapLineToDocument()
    {
        return ShoppingCartMapping.MapLineToDocument(_line);
    }
}