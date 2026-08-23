using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Domain
{
    internal static class ShoppingCartLineValidationDataFactory
    {
        public static IReadOnlyCollection<ShoppingCartLine> CreateValid()
        {
            return
            [
                new ShoppingCartLine(Guid.NewGuid(), 1),
                new ShoppingCartLine(Guid.NewGuid(), 2)
            ];
        }

        public static IReadOnlyCollection<ShoppingCartLine> CreateSingleError()
        {
            return
            [
                new ShoppingCartLine(Guid.NewGuid(), 0)
            ];
        }

        public static IReadOnlyCollection<ShoppingCartLine> CreateMultipleErrors()
        {
            return
            [
                new ShoppingCartLine(Guid.NewGuid(), 0)
            ];
        }
    }
}
