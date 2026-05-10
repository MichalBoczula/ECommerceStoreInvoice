using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Orders.Domain
{
    internal static class OrderValidationDataFactory
    {
        public static Order CreateValid()
        {
            return new Order(
                Guid.NewGuid(),
                [CreateOrderLine()]);
        }

        public static Order CreateInvalidOrderLines()
        {
            return new Order(
                Guid.NewGuid(),
                []);
        }

        public static Order CreateAllInvalid()
        {
            return new Order(
                Guid.Empty,
                []);
        }

        private static OrderLine CreateOrderLine()
        {
            return new OrderLine(
                Guid.NewGuid(),
                "Product",
                "Brand",
                new Money(100m, "USD"),
                2);
        }
    }
}
