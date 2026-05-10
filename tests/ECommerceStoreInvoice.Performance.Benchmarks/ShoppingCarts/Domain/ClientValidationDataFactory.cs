using System;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ShoppingCarts.Domain
{
    internal static class ClientValidationDataFactory
    {
        public static Guid CreateValid()
        {
            return Guid.NewGuid();
        }

        public static Guid CreateInvalid()
        {
            return Guid.Empty;
        }

        public static Guid CreateAllInvalid()
        {
            return Guid.Empty;
        }
    }
}
