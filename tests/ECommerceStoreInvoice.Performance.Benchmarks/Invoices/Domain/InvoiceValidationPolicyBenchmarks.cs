using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Domain
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class InvoiceValidationPolicyBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IServiceScope _serviceScope = null!;

        private IValidationPolicy<InvoiceOrderStatusValidationContext> _policy = null!;

        private InvoiceOrderStatusValidationContext _validContext;
        private InvoiceOrderStatusValidationContext _invalidCreatedOrderContext;
        private InvoiceOrderStatusValidationContext _invalidCancelledOrderContext;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddScoped<IValidationPolicy<InvoiceOrderStatusValidationContext>, InvoiceValidationPolicy>();

            _serviceProvider = services.BuildServiceProvider();
            _serviceScope = _serviceProvider.CreateScope();

            _policy = _serviceScope.ServiceProvider.GetRequiredService<IValidationPolicy<InvoiceOrderStatusValidationContext>>();

            _validContext = InvoiceValidationDataFactory.CreateValid();
            _invalidCreatedOrderContext = InvoiceValidationDataFactory.CreateInvalidCreatedOrder();
            _invalidCancelledOrderContext = InvoiceValidationDataFactory.CreateInvalidCancelledOrder();
        }

        [Benchmark(Baseline = true)]
        public async Task Validate_Success_HappyPath()
        {
            await _policy.Validate(_validContext);
        }

        [Benchmark]
        public async Task Validate_Failure_OrderStatusCreated()
        {
            await _policy.Validate(_invalidCreatedOrderContext);
        }

        [Benchmark]
        public async Task Validate_Failure_OrderStatusCancelled()
        {
            await _policy.Validate(_invalidCancelledOrderContext);
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
