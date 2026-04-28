using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures;

[MemoryDiagnoser]
public class ClientDataVersionMappingBenchmarks
{
    private ClientDataVersionDocument _document = null!;
    private ClientDataVersion _domain = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = ClientDataVersionDocumentBenchmarkDataFactory.Create();
        _domain = ClientDataVersionMapping.MapToDomain(_document);
    }

    [Benchmark]
    public ClientDataVersion MapDocumentToDomain()
    {
        return ClientDataVersionMapping.MapToDomain(_document);
    }

    [Benchmark]
    public object MapDomainToDocument()
    {
        return ClientDataVersionMapping.MapToDocument(_domain);
    }
}
