using BenchmarkDotNet.Running;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Domain;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures;

namespace ECommerceStoreInvoice.Performance.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        // 1. ShoppingCarts (Mapping & Database)
        BenchmarkRunner.Run<ShoppingCartMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartMappingBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartRepositoryBenchmarks>();

        // 2. Orders (Mapping & Database)
        BenchmarkRunner.Run<OrderMappingConfigBenchmarks>();
        BenchmarkRunner.Run<OrderMappingBenchmarks>();
        BenchmarkRunner.Run<OrderRepositoryBenchmarks>();

        // 3. Invoices (Mapping & Database)
        BenchmarkRunner.Run<InvoiceMappingConfigBenchmarks>();
        BenchmarkRunner.Run<InvoiceMappingBenchmarks>();
        BenchmarkRunner.Run<InvoiceRepositoryBenchmarks>();

        // 4. ClientDataVersions (Mapping & Database)
        BenchmarkRunner.Run<ClientDataVersionMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionMappingBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionRepositoryBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionValidationBenchmarks>();

        // 5. ProductVersions (Mapping & Database)
        BenchmarkRunner.Run<ProductVersionMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ProductVersionMappingBenchmarks>();
        BenchmarkRunner.Run<ProductVersionRepositoryBenchmarks>();
    }
}