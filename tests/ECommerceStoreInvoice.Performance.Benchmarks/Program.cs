using BenchmarkDotNet.Running;
using ECommerceStoreInvoice.Performance.Benchmarks.Benchmarks;

namespace ECommerceStoreInvoice.Performance.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<ShoppingCartMappingBenchmarks>();
        }
    }
}
