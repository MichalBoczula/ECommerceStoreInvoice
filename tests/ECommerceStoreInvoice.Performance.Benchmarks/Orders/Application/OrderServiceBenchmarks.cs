using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;
using ECommerceStoreInvoice.Application.Services.Concrete.Orders;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Application
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class OrderServiceBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IOrderService _service = null!;

        // Mocki dla wszystkich zależności
        private readonly Mock<IOrderRepository> _orderRepoMock = new();
        private readonly Mock<IProductVersionRepository> _productRepoMock = new();
        private readonly Mock<IShoppingCartRepository> _cartRepoMock = new();
        private readonly Mock<IValidationPolicy<Guid>> _guidPolicyMock = new();
        private readonly Mock<IValidationPolicy<Order>> _orderPolicyMock = new();
        private readonly Mock<IValidationPolicy<(Order order, OrderStatus newStatus)>> _updatePolicyMock = new();

        private Guid _clientId;
        private Guid _orderId;
        private ShoppingCart _sampleCart = null!;
        private Order _sampleOrder = null!;
        private UpdateOrderStatusRequestDto _updateRequest = null!;

        [Params(1, 10, 100)]
        public int LinesCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _clientId = Guid.NewGuid();
            _orderId = Guid.NewGuid();

            _sampleCart = OrderMappingConfigBenchmarkDataFactory.CreateDomainCart(LinesCount);
            _sampleOrder = OrderMappingConfigBenchmarkDataFactory.CreateDomainOrder(LinesCount);
            _updateRequest = new UpdateOrderStatusRequestDto { Status = "Processing" };

            _guidPolicyMock.Setup(x => x.Validate(It.IsAny<Guid>())).ReturnsAsync(new ValidationResult());
            _orderPolicyMock.Setup(x => x.Validate(It.IsAny<Order>())).ReturnsAsync(new ValidationResult());
            _updatePolicyMock.Setup(x => x.Validate(It.IsAny<(Order, OrderStatus)>())).ReturnsAsync(new ValidationResult());

            _cartRepoMock.Setup(x => x.GetShoppingCartByClientId(It.IsAny<Guid>())).ReturnsAsync(_sampleCart);
            _cartRepoMock.Setup(x => x.UpdateShoppingCart(It.IsAny<ShoppingCart>())).ReturnsAsync(_sampleCart);

            _orderRepoMock.Setup(x => x.CreateOrder(It.IsAny<Order>())).ReturnsAsync(_sampleOrder);
            _orderRepoMock.Setup(x => x.GetOrderByOrderId(It.IsAny<Guid>())).ReturnsAsync(_sampleOrder);
            _orderRepoMock.Setup(x => x.GetOrdersByClientId(It.IsAny<Guid>())).ReturnsAsync(new[] { _sampleOrder });

            foreach (var line in _sampleCart.Lines)
            {
                var productVersion = OrderMappingConfigBenchmarkDataFactory.CreateProductVersion(1);
                _productRepoMock.Setup(x => x.GetProductVersionById(line.ProductId)).ReturnsAsync(productVersion);
            }

            var services = new ServiceCollection();
            services.AddScoped<IOrderService, OrderService>();

            services.AddSingleton(_orderRepoMock.Object);
            services.AddSingleton(_productRepoMock.Object);
            services.AddSingleton(_cartRepoMock.Object);
            services.AddSingleton(_guidPolicyMock.Object);
            services.AddSingleton(_orderPolicyMock.Object);
            services.AddSingleton(_updatePolicyMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _service = _serviceProvider.GetRequiredService<IOrderService>();
        }

        [Benchmark(Baseline = true)]
        public async Task CreateOrder_FullFlow()
        {
            await _service.CreateOrder(_clientId);
        }

        [Benchmark]
        public async Task GetOrdersByClientId_Flow()
        {
            await _service.GetOrdersByClientId(_clientId);
        }

        [Benchmark]
        public async Task UpdateOrderStatus_Flow()
        {
            await _service.UpdateOrderStatus(_orderId, _updateRequest);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        }
    }
}
