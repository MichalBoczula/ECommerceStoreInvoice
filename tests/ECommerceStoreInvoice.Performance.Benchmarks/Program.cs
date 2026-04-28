using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Infrastructures;

namespace ECommerceStoreInvoice.Performance.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher
                .FromTypes([typeof(ShoppingCartMappingBenchmarks)])
                .Run(args, DefaultConfig.Instance);
        }
    }
}
