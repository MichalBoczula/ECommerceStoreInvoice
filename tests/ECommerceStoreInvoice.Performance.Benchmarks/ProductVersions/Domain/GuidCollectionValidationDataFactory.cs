namespace ECommerceStoreInvoice.Performance.Benchmarks.ProductVersions.Domain
{
    internal static class GuidCollectionValidationDataFactory
    {
        public static IEnumerable<Guid> CreateValid()
        {
            return [Guid.NewGuid(), Guid.NewGuid()];
        }

        public static IEnumerable<Guid> CreateEmpty()
        {
            return [];
        }

        public static IEnumerable<Guid> CreateContainingEmptyGuid()
        {
            return [Guid.NewGuid(), Guid.Empty];
        }
    }
}
