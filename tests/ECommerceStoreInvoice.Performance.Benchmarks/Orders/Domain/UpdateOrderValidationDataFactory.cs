using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Domain
{
    internal static class UpdateOrderValidationDataFactory
    {
        public static (Order order, OrderStatus newStatus) CreateValid()
        {
            return (CreateOrderWithStatus(OrderStatus.Created), OrderStatus.Paid);
        }

        public static (Order order, OrderStatus newStatus) CreateInvalidTransition()
        {
            return (CreateOrderWithStatus(OrderStatus.Paid), OrderStatus.Cancelled);
        }

        public static (Order order, OrderStatus newStatus) CreateNoOpTransition()
        {
            return (CreateOrderWithStatus(OrderStatus.Created), OrderStatus.Created);
        }

        private static Order CreateOrderWithStatus(OrderStatus status)
        {
            var line = new OrderLine(Guid.NewGuid(), "Keyboard", "Logi", new Money(100, "USD"), 1);

            return Order.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [line],
                DateTime.UtcNow,
                DateTime.UtcNow,
                status,
                new Money(100, "USD"));
        }
    }
}
