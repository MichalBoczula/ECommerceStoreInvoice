using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application;

[MemoryDiagnoser]
public class ProductVersionMappingConfigBenchmarks
{
    private ProductVersion _productVersion = null!;

    [GlobalSetup]
    public void Setup()
    {
        _productVersion = ProductVersionMappingConfigBenchmarkDataFactory.CreateDomainProductVersion();
    }

    [Benchmark]
    public object MapToResponse()
    {
        return MappingConfig.MapToResponse(_productVersion);
    }
}
