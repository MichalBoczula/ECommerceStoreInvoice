using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Domain
{
    internal static class ShoppingCartLineValidationDataFactory
    {
        public static IReadOnlyCollection<ShoppingCartLine> CreateValid()
        {
            return
            [
                new ShoppingCartLine(Guid.NewGuid(), "Keyboard", "Logi", new Money(99, "USD"), 1),
                new ShoppingCartLine(Guid.NewGuid(), "Mouse", "Logi", new Money(49, "USD"), 2)
            ];
        }

        public static IReadOnlyCollection<ShoppingCartLine> CreateSingleError()
        {
            return
            [
                new ShoppingCartLine(Guid.NewGuid(), "Keyboard", "Logi", new Money(99, "USD"), 0)
            ];
        }

        public static IReadOnlyCollection<ShoppingCartLine> CreateMultipleErrors()
        {
            return
            [
                new ShoppingCartLine(Guid.NewGuid(), "", "", new Money(-10, "EUR"), 0)
            ];
        }
    }
}
