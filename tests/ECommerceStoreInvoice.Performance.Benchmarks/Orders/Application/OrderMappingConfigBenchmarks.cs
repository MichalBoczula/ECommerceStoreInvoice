using System.Reflection;
using BenchmarkDotNet.Attributes;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application;

[MemoryDiagnoser]
public class OrderMappingConfigBenchmarks
{
    [Params(1, 10, 100)]
    public int LinesCount { get; set; }

    private static readonly MethodInfo MapToResponseLineMethod = typeof(MappingConfig)
        .GetMethod("MapToResponse", BindingFlags.NonPublic | BindingFlags.Static, [typeof(OrderLine)])!;

    private static readonly MethodInfo MapToDomainLineMethod = typeof(MappingConfig)
        .GetMethod("MapToDomain", BindingFlags.NonPublic | BindingFlags.Static, [typeof(ShoppingCartLine), typeof(ProductVersion)])!;

    private OrderLine _orderLine = null!;
    private ShoppingCartLine _shoppingCartLine = null!;
    private ProductVersion _productVersion = null!;
    private Order _order = null!;
    private ShoppingCart _shoppingCart = null!;
    private IReadOnlyCollection<ProductVersion> _productVersions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _shoppingCart = OrderMappingConfigBenchmarkDataFactory.CreateDomainCart(LinesCount);
        _productVersions = OrderMappingConfigBenchmarkDataFactory.CreateProductVersions(LinesCount);
        _order = OrderMappingConfigBenchmarkDataFactory.CreateDomainOrder(LinesCount);
        _shoppingCartLine = _shoppingCart.Lines.First();
        _productVersion = _productVersions.First();
        _orderLine = _order.Lines.First();
    }

    [Benchmark]
    public object MapLineToResponse()
    {
        return MapToResponseLineMethod.Invoke(null, [_orderLine])!;
    }

    [Benchmark]
    public object MapCartLineAndProductVersionToDomain()
    {
        return MapToDomainLineMethod.Invoke(null, [_shoppingCartLine, _productVersion])!;
    }

    [Benchmark]
    public OrderResponseDto MapOrderToResponse()
    {
        return MappingConfig.MapToResponse(_order);
    }

    [Benchmark]
    public Order MapCartAndProductVersionsToDomain()
    {
        return MappingConfig.MapToDomain(_shoppingCart, _productVersions);
    }
}
