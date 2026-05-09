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
        // 1. General application mapping benchmarks
        BenchmarkRunner.Run<MappingConfigBenchmarks>();

        // 2. ShoppingCarts (Mapping & Database)
        BenchmarkRunner.Run<ShoppingCartMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartMappingBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartRepositoryBenchmarks>();

        // 3. Orders (Mapping & Database)
        BenchmarkRunner.Run<OrderMappingConfigBenchmarks>();
        BenchmarkRunner.Run<OrderMappingBenchmarks>();
        BenchmarkRunner.Run<OrderRepositoryBenchmarks>();

        // 4. Invoices (Mapping & Database)
        BenchmarkRunner.Run<InvoiceMappingConfigBenchmarks>();
        BenchmarkRunner.Run<InvoiceMappingBenchmarks>();
        BenchmarkRunner.Run<InvoiceRepositoryBenchmarks>();

        // 5. ClientDataVersions (Mapping & Database)
        BenchmarkRunner.Run<ClientDataVersionMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionMappingBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionRepositoryBenchmarks>();

        // 6. ProductVersions (Mapping & Database)
        BenchmarkRunner.Run<ProductVersionMappingBenchmarks>();
        BenchmarkRunner.Run<ProductVersionRepositoryBenchmarks>();
    }
}