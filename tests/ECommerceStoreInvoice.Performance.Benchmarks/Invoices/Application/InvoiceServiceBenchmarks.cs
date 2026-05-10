using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class InvoiceServiceBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IInvoiceService _service = null!;

        // Mocki
        private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
        private readonly Mock<IOrderRepository> _orderRepoMock = new();
        private readonly Mock<IClientDataVersionService> _clientServiceMock = new();
        private readonly Mock<IInvoicePdfService> _pdfServiceMock = new();
        private readonly Mock<IValidationPolicy<Guid>> _guidPolicyMock = new();
        private readonly Mock<IValidationPolicy<InvoiceOrderStatusValidationContext>> _statusPolicyMock = new();

        private Guid _clientId;
        private Guid _orderId;
        private Guid _invoiceId;
        private Order _sampleOrder = null!;
        private Invoice _sampleInvoice = null!;
        private ClientDataVersionResponseDto _clientResponse = null!;

        [GlobalSetup]
        public void Setup()
        {
            _clientId = Guid.NewGuid();
            _orderId = Guid.NewGuid();
            _invoiceId = Guid.NewGuid();

            _sampleOrder = InvoiceMappingConfigBenchmarkDataFactory.CreateSampleOrder(_clientId, _orderId);
            _sampleInvoice = InvoiceMappingConfigBenchmarkDataFactory.CreateDomainInvoice();
            _clientResponse = InvoiceMappingConfigBenchmarkDataFactory.CreateClientResponse(_clientId);

            _guidPolicyMock.Setup(x => x.Validate(It.IsAny<Guid>())).ReturnsAsync(new ValidationResult());
            _statusPolicyMock.Setup(x => x.Validate(It.IsAny<InvoiceOrderStatusValidationContext>())).ReturnsAsync(new ValidationResult());

            _orderRepoMock.Setup(x => x.GetOrderByOrderId(_orderId)).ReturnsAsync(_sampleOrder);
            _invoiceRepoMock.Setup(x => x.GetInvoiceByOrderId(_orderId)).ReturnsAsync((Invoice?)null); // Brak faktury przy Create
            _invoiceRepoMock.Setup(x => x.GetInvoiceById(_invoiceId)).ReturnsAsync(_sampleInvoice);
            _invoiceRepoMock.Setup(x => x.CreateInvoice(It.IsAny<Invoice>())).ReturnsAsync(_sampleInvoice);

            _clientServiceMock.Setup(x => x.GetByClientId(_clientId)).ReturnsAsync(_clientResponse);
            _pdfServiceMock.Setup(x => x.GenerateInvoicePdf(It.IsAny<Order>(), It.IsAny<ClientDataVersionResponseDto>()))
                .ReturnsAsync("https://storage.example/invoices/42.pdf");

            var services = new ServiceCollection();
            services.AddScoped<IInvoiceService, InvoiceService>();

            services.AddSingleton(_invoiceRepoMock.Object);
            services.AddSingleton(_orderRepoMock.Object);
            services.AddSingleton(_clientServiceMock.Object);
            services.AddSingleton(_pdfServiceMock.Object);
            services.AddSingleton(_guidPolicyMock.Object);
            services.AddSingleton(_statusPolicyMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _service = _serviceProvider.GetRequiredService<IInvoiceService>();
        }

        [Benchmark(Baseline = true)]
        public async Task CreateInvoiceForOrder_FullFlow()
        {
            await _service.CreateInvoiceForOrder(_clientId, _orderId);
        }

        [Benchmark]
        public async Task GetInvoiceById_Flow()
        {
            await _service.GetInvoiceById(_invoiceId);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        }
    }
}
