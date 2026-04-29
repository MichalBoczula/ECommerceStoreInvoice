using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures;

[MemoryDiagnoser]
public class ProductVersionMappingBenchmarks
{
    private ProductVersionDocument _document = null!;
    private ProductVersion _domain = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = ProductVersionDocumentBenchmarkDataFactory.Create();
        _domain = ProductVersionMapping.MapToDomain(_document);
    }

    [Benchmark]
    public ProductVersion MapDocumentToDomain()
    {
        return ProductVersionMapping.MapToDomain(_document);
    }

    [Benchmark]
    public object MapDomainToDocument()
    {
        return ProductVersionMapping.MapToDocument(_domain);
    }
}
