using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Concrete.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Application
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ClientDataVersionServiceBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IClientDataVersionService _service = null!;

        private readonly Mock<IClientDataVersionRepository> _repositoryMock = new();
        private readonly Mock<IValidationPolicy<Guid>> _guidPolicyMock = new();
        private readonly Mock<IValidationPolicy<ClientDataVersion>> _domainPolicyMock = new();

        private Guid _clientId;
        private CreateClientDataVersionRequestDto _requestDto = null!;
        private ClientDataVersion _domainEntity = null!;

        [GlobalSetup]
        public void Setup()
        {
            _clientId = Guid.NewGuid();

            _requestDto = ClientDataVersionMappingConfigBenchmarkDataFactory.CreateRequest();
            _domainEntity = ClientDataVersionMappingConfigBenchmarkDataFactory.CreateDomainClientDataVersion(_clientId);

            _guidPolicyMock
                .Setup(x => x.Validate(It.IsAny<Guid>()))
                .ReturnsAsync(new ValidationResult());

            _domainPolicyMock
                .Setup(x => x.Validate(It.IsAny<ClientDataVersion>()))
                .ReturnsAsync(new ValidationResult());

            _repositoryMock
                .Setup(x => x.Create(It.IsAny<ClientDataVersion>()))
                .Returns(Task.CompletedTask);

            _repositoryMock
                .Setup(x => x.GetByClientId(_clientId))
                .ReturnsAsync(_domainEntity);

            var services = new ServiceCollection();

            services.AddScoped<IClientDataVersionService, ClientDataVersionService>();

            services.AddSingleton(_repositoryMock.Object);
            services.AddSingleton(_guidPolicyMock.Object);
            services.AddSingleton(_domainPolicyMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _service = _serviceProvider.GetRequiredService<IClientDataVersionService>();
        }

        [Benchmark(Baseline = true)]
        public async Task Create_FullFlow()
        {
            await _service.Create(_clientId, _requestDto);
        }

        [Benchmark]
        public async Task GetByClientId_FullFlow()
        {
            await _service.GetByClientId(_clientId);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
