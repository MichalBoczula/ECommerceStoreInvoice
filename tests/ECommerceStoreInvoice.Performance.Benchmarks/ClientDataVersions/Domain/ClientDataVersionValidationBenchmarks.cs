using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Domain
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ClientDataVersionValidationBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IServiceScope _serviceScope = null!;

        private IValidationPolicy<ClientDataVersion> _policy = null!;

        private ClientDataVersion _validEntity = null!;
        private ClientDataVersion _invalidEmailEntity = null!;
        private ClientDataVersion _allInvalidEntity = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddScoped<IValidationPolicy<ClientDataVersion>, ClientDataVersionValidationPolicy>();

            _serviceProvider = services.BuildServiceProvider();

            _serviceScope = _serviceProvider.CreateScope();

            _policy = _serviceScope.ServiceProvider.GetRequiredService<IValidationPolicy<ClientDataVersion>>();

            _validEntity = ClientDataVersionValidationDataFactory.CreateValid();
            _invalidEmailEntity = ClientDataVersionValidationDataFactory.CreateInvalidEmail();
            _allInvalidEntity = ClientDataVersionValidationDataFactory.CreateAllInvalid();
        }

        [Benchmark(Baseline = true)]
        public async Task Validate_Success_HappyPath()
        {
            await _policy.Validate(_validEntity);
        }

        [Benchmark]
        public async Task Validate_Failure_SingleError()
        {
            await _policy.Validate(_invalidEmailEntity);
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
