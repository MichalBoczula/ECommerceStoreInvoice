using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Concrete.ProductVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Application
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ProductVersionServiceBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IProductVersionService _service = null!;

        private readonly Mock<IProductVersionRepository> _repositoryMock = new();
        private readonly Mock<IValidationPolicy<ProductVersion>> _productPolicyMock = new();
        private readonly Mock<IValidationPolicy<Guid>> _guidPolicyMock = new();

        private Guid _id;
        private CreateProductVersionRequestDto _requestDto = null!;
        private ProductVersion _domainEntity = null!;

        [GlobalSetup]
        public void Setup()
        {
            _id = Guid.NewGuid();
            _requestDto = ProductVersionMappingConfigBenchmarkDataFactory.CreateRequest();
            _domainEntity = ProductVersionMappingConfigBenchmarkDataFactory.CreateDomainProductVersion();

            _guidPolicyMock
                .Setup(x => x.Validate(It.IsAny<Guid>()))
                .ReturnsAsync(new ValidationResult());

            _productPolicyMock
                .Setup(x => x.Validate(It.IsAny<ProductVersion>()))
                .ReturnsAsync(new ValidationResult());

            _repositoryMock
                .Setup(x => x.CreateProductVersion(It.IsAny<ProductVersion>()))
                .ReturnsAsync(_domainEntity);

            _repositoryMock
                .Setup(x => x.GetProductVersionById(It.IsAny<Guid>()))
                .ReturnsAsync(_domainEntity);

            var services = new ServiceCollection();
            services.AddScoped<IProductVersionService, ProductVersionService>();

            services.AddSingleton(_repositoryMock.Object);
            services.AddSingleton(_productPolicyMock.Object);
            services.AddSingleton(_guidPolicyMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _service = _serviceProvider.GetRequiredService<IProductVersionService>();
        }

        [Benchmark(Baseline = true)]
        public async Task CreateProductVersion_Flow()
        {
            await _service.CreateProductVersion(_requestDto);
        }

        [Benchmark]
        public async Task GetProductVersionById_Flow()
        {
            await _service.GetProductVersionById(_id);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
