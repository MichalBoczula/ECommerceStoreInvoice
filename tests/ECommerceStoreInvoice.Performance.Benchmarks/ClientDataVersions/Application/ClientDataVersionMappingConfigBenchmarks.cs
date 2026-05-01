using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application;

[MemoryDiagnoser]
public class ClientDataVersionMappingConfigBenchmarks
{
    private Guid _clientId;
    private CreateClientDataVersionRequestDto _request = null!;
    private ClientDataVersion _clientDataVersion = null!;

    [GlobalSetup]
    public void Setup()
    {
        _clientId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        _request = ClientDataVersionMappingConfigBenchmarkDataFactory.CreateRequest();
        _clientDataVersion = ClientDataVersionMappingConfigBenchmarkDataFactory.CreateDomainClientDataVersion(_clientId);
    }

    [Benchmark]
    public object MapToDomain()
    {
        return MappingConfig.MapToDomain(_clientId, _request);
    }

    [Benchmark]
    public object MapToResponse()
    {
        return MappingConfig.MapToResponse(_clientDataVersion);
    }
}
