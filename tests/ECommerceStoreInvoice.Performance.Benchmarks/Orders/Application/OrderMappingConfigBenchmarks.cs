using System.Reflection;
using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application;

[MemoryDiagnoser]
public class OrderMappingConfigBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private static readonly MethodInfo MapToResponseLineMethod = typeof(MappingConfig)
        .GetMethod("MapToResponse", BindingFlags.Public | BindingFlags.Static, [typeof(OrderLine), typeof(ProductVersionResponseDto)])!;

    private static readonly MethodInfo MapToDomainLineMethod = typeof(MappingConfig)
        .GetMethod("MapToDomain", BindingFlags.NonPublic | BindingFlags.Static, [typeof(ShoppingCartLine), typeof(ProductVersion)])!;

    private OrderLine _orderLine = null!;
    private ShoppingCartLine _shoppingCartLine = null!;
    private ProductVersion _productVersion = null!;
    private ProductVersionResponseDto _productVersionResponse = null!;
    private Order _order = null!;
    private ShoppingCart _shoppingCart = null!;
    private IReadOnlyCollection<ProductVersion> _productVersions = null!;
    private IReadOnlyCollection<ProductVersionResponseDto> _productVersionResponses = null!;

    [GlobalSetup]
    public void Setup()
    {
        _shoppingCart = OrderMappingConfigBenchmarkDataFactory.CreateDomainCart(LinesCount);
        _productVersions = OrderMappingConfigBenchmarkDataFactory.CreateProductVersions(LinesCount);
        _productVersionResponses = _productVersions.Select(MappingConfig.MapToResponse).ToList();
        _order = OrderMappingConfigBenchmarkDataFactory.CreateDomainOrder(LinesCount);
        _shoppingCartLine = _shoppingCart.Lines.First();
        _productVersion = _productVersions.First();
        _productVersionResponse = _productVersionResponses.First();
        _orderLine = _order.Lines.First();
    }

    [Benchmark]
    public object MapLineToResponse()
    {
        return MapToResponseLineMethod.Invoke(null, [_orderLine, _productVersionResponse])!;
    }

    [Benchmark]
    public object MapCartLineAndProductVersionToDomain()
    {
        return MapToDomainLineMethod.Invoke(null, [_shoppingCartLine, _productVersion])!;
    }

    [Benchmark]
    public object MapOrderToResponse()
    {
        return MappingConfig.MapToResponse(_order, _productVersionResponses);
    }

    [Benchmark]
    public object MapCartAndProductVersionsToDomain()
    {
        return MappingConfig.MapToDomain(_shoppingCart, _productVersions);
    }
}