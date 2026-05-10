using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Concrete.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Application
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ShoppingCartServiceBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IShoppingCartService _service = null!;

        private readonly Mock<IShoppingCartRepository> _repositoryMock = new();
        private readonly Mock<IValidationPolicy<IReadOnlyCollection<ShoppingCartLine>>> _linesPolicyMock = new();
        private readonly Mock<IValidationPolicy<Guid>> _guidPolicyMock = new();

        private Guid _clientId;
        private ShoppingCart _sampleCart = null!;

        [Params(1, 10, 100)]
        public int LinesCount { get; set; }

        private UpdateShoppingCartRequestDto _updateRequest = null!;

        [GlobalSetup]
        public void Setup()
        {
            _clientId = Guid.NewGuid();
            _sampleCart = ShoppingCartMappingConfigBenchmarkDataFactory.CreateDomainCart(LinesCount);

            _updateRequest = new UpdateShoppingCartRequestDto
            {
                Lines = ShoppingCartMappingConfigBenchmarkDataFactory.CreateRequestLines(LinesCount)
            };

            _guidPolicyMock
                .Setup(x => x.Validate(It.IsAny<Guid>()))
                .ReturnsAsync(new ValidationResult());

            _linesPolicyMock
                .Setup(x => x.Validate(It.IsAny<IReadOnlyCollection<ShoppingCartLine>>()))
                .ReturnsAsync(new ValidationResult());

            _repositoryMock
                .Setup(x => x.GetShoppingCartByClientId(It.IsAny<Guid>()))
                .ReturnsAsync(_sampleCart);

            _repositoryMock
                .Setup(x => x.CreateShoppingCart(It.IsAny<ShoppingCart>()))
                .ReturnsAsync(_sampleCart);

            _repositoryMock
                .Setup(x => x.UpdateShoppingCart(It.IsAny<ShoppingCart>()))
                .ReturnsAsync(_sampleCart);

            var services = new ServiceCollection();
            services.AddScoped<IShoppingCartService, ShoppingCartService>();

            services.AddSingleton(_repositoryMock.Object);
            services.AddSingleton(_linesPolicyMock.Object);
            services.AddSingleton(_guidPolicyMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _service = _serviceProvider.GetRequiredService<IShoppingCartService>();
        }

        [Benchmark]
        public async Task GetShoppingCartByClientId_Flow()
        {
            await _service.GetShoppingCartByClientId(_clientId);
        }

        [Benchmark]
        public async Task CreateShoppingCart_Flow()
        {
            await _service.CreateShoppingCart(_clientId);
        }

        [Benchmark]
        public async Task UpdateShoppingCart_FullFlow()
        {
            await _service.UpdateShoppingCart(_clientId, _updateRequest);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
