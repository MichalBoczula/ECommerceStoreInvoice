using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Domain
{
    internal static class InvoiceValidationDataFactory
    {
        public static InvoiceOrderStatusValidationContext CreateValid()
        {
            return new InvoiceOrderStatusValidationContext(CreateOrder(OrderStatus.Paid));
        }

        public static InvoiceOrderStatusValidationContext CreateInvalidCreatedOrder()
        {
            return new InvoiceOrderStatusValidationContext(CreateOrder(OrderStatus.Created));
        }

        public static InvoiceOrderStatusValidationContext CreateInvalidCancelledOrder()
        {
            return new InvoiceOrderStatusValidationContext(CreateOrder(OrderStatus.Cancelled));
        }

        private static Order CreateOrder(OrderStatus status)
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
