using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Domain
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ShoppingCartLineValidationBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IServiceScope _serviceScope = null!;

        private IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>> _policy = null!;

        private IReadOnlyCollection<ShoppingCartLine> _validEntity = null!;
        private IReadOnlyCollection<ShoppingCartLine> _singleErrorEntity = null!;
        private IReadOnlyCollection<ShoppingCartLine> _multipleErrorsEntity = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddScoped<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>, ShoppingCartLineValidationPolicy>();

            _serviceProvider = services.BuildServiceProvider();

            _serviceScope = _serviceProvider.CreateScope();

            _policy = _serviceScope.ServiceProvider.GetRequiredService<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>>();

            _validEntity = ShoppingCartLineValidationDataFactory.CreateValid();
            _singleErrorEntity = ShoppingCartLineValidationDataFactory.CreateSingleError();
            _multipleErrorsEntity = ShoppingCartLineValidationDataFactory.CreateMultipleErrors();
        }

        [Benchmark(Baseline = true)]
        public async Task Validate_Success_HappyPath()
        {
            await _policy.Validate(_validEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_SingleError()
        {
            await _policy.Validate(_singleErrorEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_MultipleErrors()
        {
            await _policy.Validate(_multipleErrorsEntity);
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
