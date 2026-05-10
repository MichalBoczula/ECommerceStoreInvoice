using System;

namespace ECommerceStoreInvoice.Performance.Benchmarks.ClientDataVersions.Domain
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
