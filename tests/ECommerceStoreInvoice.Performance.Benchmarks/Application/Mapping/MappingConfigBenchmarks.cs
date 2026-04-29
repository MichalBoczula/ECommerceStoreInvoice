using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Performance.Benchmarks.Application.Mapping.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Application.Mapping;

[MemoryDiagnoser]
public class MappingConfigBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private Guid _clientId;
    private CreateClientDataVersionRequestDto _clientDataVersionRequest = null!;
    private ShoppingCart _shoppingCart = null!;

    [GlobalSetup]
    public void Setup()
    {
        _clientId = Guid.NewGuid();
        _clientDataVersionRequest = MappingConfigBenchmarkDataFactory.CreateClientDataVersionRequest();
        _shoppingCart = MappingConfigBenchmarkDataFactory.CreateShoppingCartWithLines(LinesCount);
    }

    [Benchmark]
    public object MapClientDataVersionRequestToDomain()
    {
        return MappingConfig.MapToDomain(_clientId, _clientDataVersionRequest);
    }

    [Benchmark]
    public object MapShoppingCartToResponse()
    {
        return MappingConfig.MapToResponse(_shoppingCart);
    }
}
