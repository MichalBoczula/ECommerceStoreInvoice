using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;

namespace ECommerceStoreInvoice.Infrastructure.Mapping
{
    internal static class OrderMapping
    {
        internal static OrderDocument MapToDocument(Order order)
        {
            return new OrderDocument
            {
                Id = order.Id,
                ClientId = order.ClientId,
                Lines = order.Lines.Select(MapLineToDocument).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Status = order.Status
            };
        }

        internal static Order MapToDomain(OrderDocument orderDocument)
        {
            var lines = orderDocument.Lines.Select(x =>
                new OrderLine(
                    x.ProductVersionId,
                    x.Quantity)).ToList();

            return Order.Rehydrate(
                orderDocument.Id,
                orderDocument.ClientId,
                lines,
                orderDocument.CreatedAt,
                orderDocument.UpdatedAt,
                orderDocument.Status);
        }

        internal static (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) MapToDomain(
            OrderWithProductsDocument document)
        {
            var lines = document.Lines.Select(x =>
                new OrderLine(
                    x.ProductVersionId,
                    x.Quantity)).ToList();

            var order = Order.Rehydrate(
                document.Id,
                document.ClientId,
                lines,
                document.CreatedAt,
                document.UpdatedAt,
                document.Status);

            var productVersions = document.ProductVersions
                .Select(ProductVersionMapping.MapToDomain)
                .ToList();

            return (order, productVersions);
        }

        private static OrderLineDocument MapLineToDocument(OrderLine orderLine)
        {
            return new OrderLineDocument
            {
                ProductVersionId = orderLine.ProductVersionId,
                Quantity = orderLine.Quantity
            };
        }
    }
}