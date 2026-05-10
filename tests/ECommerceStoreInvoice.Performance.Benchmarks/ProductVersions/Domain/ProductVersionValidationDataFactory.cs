using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Domain
{
    internal static class ProductVersionValidationDataFactory
    {
        public static ProductVersion CreateValid()
        {
            return new ProductVersion(
                Guid.NewGuid(),
                new Money(100m, "USD"),
                "Product Name",
                "Product Brand");
        }

        public static ProductVersion CreateInvalidCurrency()
        {
            return new ProductVersion(
                Guid.NewGuid(),
                new Money(100m, "EUR"),
                "Product Name",
                "Product Brand");
        }

        public static ProductVersion CreateAllInvalid()
        {
            return new ProductVersion(
                Guid.Empty,
                new Money(-100m, "   "),
                " ",
                " ");
        }
    }
}
