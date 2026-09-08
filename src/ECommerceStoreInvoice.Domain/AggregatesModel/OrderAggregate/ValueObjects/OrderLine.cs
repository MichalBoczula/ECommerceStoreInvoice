using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;

namespace ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects
{
    public record OrderLine
    {
        public Guid ProductVersionId { get; init; }
        public int Quantity { get; init; }

        public OrderLine(Guid productVersionId, int quantity)
        {
            ProductVersionId = productVersionId;
            Quantity = quantity;
        }
    }
}