using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Domain
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class GuidCollectionValidationBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IServiceScope _serviceScope = null!;

        private IValidationPolicy<IEnumerable<Guid>> _policy = null!;

        private IEnumerable<Guid> _validEntity = null!;
        private IEnumerable<Guid> _emptyEntity = null!;
        private IEnumerable<Guid> _entityContainingEmptyGuid = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddScoped<IValidationPolicy<IEnumerable<Guid>>, GuidCollectionValidationPolicy>();

            _serviceProvider = services.BuildServiceProvider();

            _serviceScope = _serviceProvider.CreateScope();

            _policy = _serviceScope.ServiceProvider.GetRequiredService<IValidationPolicy<IEnumerable<Guid>>>();

            _validEntity = GuidCollectionValidationDataFactory.CreateValid();
            _emptyEntity = GuidCollectionValidationDataFactory.CreateEmpty();
            _entityContainingEmptyGuid = GuidCollectionValidationDataFactory.CreateContainingEmptyGuid();
        }

        [Benchmark(Baseline = true)]
        public async Task Validate_Success_HappyPath()
        {
            await _policy.Validate(_validEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_EmptyCollection()
        {
            await _policy.Validate(_emptyEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_ContainsEmptyGuid()
        {
            await _policy.Validate(_entityContainingEmptyGuid);
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
