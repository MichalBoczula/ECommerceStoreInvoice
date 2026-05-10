using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Domain
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class OrderValidationPolicyBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IServiceScope _serviceScope = null!;

        private IValidationPolicy<Order> _policy = null!;

        private Order _validEntity = null!;
        private Order _invalidOrderLinesEntity = null!;
        private Order _allInvalidEntity = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddScoped<IValidationPolicy<Order>, OrderValidationPolicy>();

            _serviceProvider = services.BuildServiceProvider();

            _serviceScope = _serviceProvider.CreateScope();

            _policy = _serviceScope.ServiceProvider.GetRequiredService<IValidationPolicy<Order>>();

            _validEntity = OrderValidationDataFactory.CreateValid();
            _invalidOrderLinesEntity = OrderValidationDataFactory.CreateInvalidOrderLines();
            _allInvalidEntity = OrderValidationDataFactory.CreateAllInvalid();
        }

        [Benchmark(Baseline = true)]
        public async Task Validate_Success_HappyPath()
        {
            await _policy.Validate(_validEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_SingleError()
        {
            await _policy.Validate(_invalidOrderLinesEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_MultipleErrors()
        {
            await _policy.Validate(_allInvalidEntity);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceScope.Dispose();
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
