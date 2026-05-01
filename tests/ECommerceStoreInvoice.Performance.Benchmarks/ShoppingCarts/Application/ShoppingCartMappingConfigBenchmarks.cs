using System.Reflection;
using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application;

[MemoryDiagnoser]
public class ShoppingCartMappingConfigBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private static readonly MethodInfo MapToResponseLineMethod = typeof(MappingConfig)
        .GetMethod("MapToResponse", BindingFlags.NonPublic | BindingFlags.Static, [typeof(ShoppingCartLine)])!;

    private static readonly MethodInfo MapToDomainLineMethod = typeof(MappingConfig)
        .GetMethod("MapToDomain", BindingFlags.NonPublic | BindingFlags.Static, [typeof(ShoppingCartLineRequestDto)])!;

    private ShoppingCartLine _shoppingCartLine = null!;
    private ShoppingCartLineRequestDto _requestLine = null!;
    private ShoppingCart _shoppingCart = null!;
    private IReadOnlyCollection<ShoppingCartLineRequestDto> _requestLines = null!;

    [GlobalSetup]
    public void Setup()
    {
        _requestLines = ShoppingCartMappingConfigBenchmarkDataFactory.CreateRequestLines(LinesCount);
        _requestLine = _requestLines.First();
        _shoppingCart = ShoppingCartMappingConfigBenchmarkDataFactory.CreateDomainCart(LinesCount);
        _shoppingCartLine = _shoppingCart.Lines.First();
    }

    [Benchmark]
    public object MapLineToResponse()
    {
        return MapToResponseLineMethod.Invoke(null, [_shoppingCartLine])!;
    }

    [Benchmark]
    public object MapRequestLineToDomain()
    {
        return MapToDomainLineMethod.Invoke(null, [_requestLine])!;
    }

    [Benchmark]
    public object MapCartToResponse()
    {
        return MappingConfig.MapToResponse(_shoppingCart);
    }

    [Benchmark]
    public object MapRequestLinesToDomain()
    {
        return MappingConfig.MapToDomain(_requestLines);
    }
}
