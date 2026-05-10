using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Domain
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class UpdateOrderValidationPolicyBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IServiceScope _serviceScope = null!;

        private IValidationPolicy<(Order order, OrderStatus newStatus)> _policy = null!;

        private (Order order, OrderStatus newStatus) _validEntity = default;
        private (Order order, OrderStatus newStatus) _invalidTransitionEntity = default;
        private (Order order, OrderStatus newStatus) _noOpTransitionEntity = default;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddScoped<IValidationPolicy<(Order order, OrderStatus newStatus)>, UpdateOrderValidationPolicy>();

            _serviceProvider = services.BuildServiceProvider();

            _serviceScope = _serviceProvider.CreateScope();

            _policy = _serviceScope.ServiceProvider.GetRequiredService<IValidationPolicy<(Order order, OrderStatus newStatus)>>();

            _validEntity = UpdateOrderValidationDataFactory.CreateValid();
            _invalidTransitionEntity = UpdateOrderValidationDataFactory.CreateInvalidTransition();
            _noOpTransitionEntity = UpdateOrderValidationDataFactory.CreateNoOpTransition();
        }

        [Benchmark(Baseline = true)]
        public async Task Validate_Success_HappyPath()
        {
            await _policy.Validate(_validEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_InvalidTransition()
        {
            await _policy.Validate(_invalidTransitionEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_NoOpTransition()
        {
            await _policy.Validate(_noOpTransitionEntity);
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
