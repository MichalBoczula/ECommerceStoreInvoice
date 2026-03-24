using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal class OrderService(
        IOrderRepository _orderRepository)
        : IOrderService
    {
    }
}
