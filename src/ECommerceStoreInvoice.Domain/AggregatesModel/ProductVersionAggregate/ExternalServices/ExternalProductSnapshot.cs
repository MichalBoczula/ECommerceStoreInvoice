using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.ExternalServices
{
    public sealed record ExternalProductSnapshot(
     Guid ProductId,
     string Name,
     string Brand,
     Money Price);
}
