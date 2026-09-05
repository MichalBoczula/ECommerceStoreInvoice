using BenchmarkDotNet.Running;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Domain;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Domain;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Domain;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Domain;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Domain;

namespace ECommerceStoreInvoice.Performance.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        // 1. ShoppingCarts (Mapping & Database & Validation)
        BenchmarkRunner.Run<ShoppingCartMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartMappingBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartRepositoryBenchmarks>();
        BenchmarkRunner.Run<ShoppingCartLineValidationBenchmarks>();

        // 2. Orders (Mapping & Database & Validation)
        BenchmarkRunner.Run<OrderMappingConfigBenchmarks>();
        BenchmarkRunner.Run<OrderMappingBenchmarks>();
        BenchmarkRunner.Run<OrderRepositoryBenchmarks>();
        BenchmarkRunner.Run<UpdateOrderValidationPolicyBenchmarks>();
        BenchmarkRunner.Run<OrderValidationPolicyBenchmarks>();

        // 3. Invoices (Mapping & Database & Validation)
        BenchmarkRunner.Run<InvoiceMappingConfigBenchmarks>();
        BenchmarkRunner.Run<InvoiceMappingBenchmarks>();
        BenchmarkRunner.Run<InvoiceRepositoryBenchmarks>();
        BenchmarkRunner.Run<InvoiceValidationPolicyBenchmarks>();

        // 4. ClientDataVersions (Mapping & Database & Validation)
        BenchmarkRunner.Run<ClientDataVersionMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionMappingBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionRepositoryBenchmarks>();
        BenchmarkRunner.Run<ClientDataVersionValidationBenchmarks>();
        BenchmarkRunner.Run<ClientValidationPolicyBenchmarks>();

        // 5. ProductVersions (Mapping & Database & Validation)
        BenchmarkRunner.Run<ProductVersionMappingConfigBenchmarks>();
        BenchmarkRunner.Run<ProductVersionMappingBenchmarks>();
        BenchmarkRunner.Run<ProductVersionRepositoryBenchmarks>();
        BenchmarkRunner.Run<ProductVersionValidationBenchmarks>();
        BenchmarkRunner.Run<GuidCollectionValidationBenchmarks>();
    }
}
