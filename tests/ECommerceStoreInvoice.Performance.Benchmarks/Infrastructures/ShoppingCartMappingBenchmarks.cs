using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;
using ECommerceStoreInvoice.Performance.Benchmarks.TestData;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ShoppingCartMappingBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private ShoppingCartDocument _document = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = ShoppingCartDocumentBenchmarkDataFactory.CreateWithLines(LinesCount);
    }

    [Benchmark]
    public ShoppingCart MapDocumentToDomain()
    {
        return ShoppingCartMapping.MapToDomain(_document);
    }
}