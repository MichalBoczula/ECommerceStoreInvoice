using BenchmarkDotNet.Running;
using ECommerceStoreInvoice.Performance.Benchmarks.Application.Mapping;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures;

namespace ECommerceStoreInvoice.Performance.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        // General application mapping benchmarks
        BenchmarkRunner.Run<MappingConfigBenchmarks>();

        // ShoppingCarts
        BenchmarkRunner.Run<ShoppingCartMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartMappingBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartRepositoryBenchmarks>();

        // Orders
        BenchmarkRunner.Run<OrderMappingConfigBenchmarks>();
        BenchmarkRunner.Run<OrderMappingBenchmarks>();

        // Invoices
        BenchmarkRunner.Run<InvoiceMappingConfigBenchmarks>();
        BenchmarkRunner.Run<InvoiceMappingBenchmarks>();

        // ClientDataVersions
        BenchmarkRunner.Run<ClientDataVersionMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionMappingBenchmarks>();

        // ProductVersions
        BenchmarkRunner.Run<ProductVersionMappingBenchmarks>();
    }
}