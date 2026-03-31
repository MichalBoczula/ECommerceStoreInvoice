using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
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
                Status = order.Status,
                TotalAmount = order.Total.Amount,
                TotalCurrency = order.Total.Currency
            };
        }

        internal static Order MapToDomain(OrderDocument orderDocument)
        {
            var lines = orderDocument.Lines.Select(x =>
                new OrderLine(
                    x.ProductVersionId,
                    x.Name,
                    x.Brand,
                    new Money(x.UnitPriceAmount, x.UnitPriceCurrency),
                    x.Quantity)).ToList();

            return Order.Rehydrate(
                orderDocument.Id,
                orderDocument.ClientId,
                lines,
                orderDocument.CreatedAt,
                orderDocument.UpdatedAt,
                orderDocument.Status,
                new Money(orderDocument.TotalAmount, orderDocument.TotalCurrency));
        }

        private static OrderLineDocument MapLineToDocument(OrderLine orderLine)
        {
            return new OrderLineDocument
            {
                ProductVersionId = orderLine.ProductVersionId,
                Name = orderLine.Name,
                Brand = orderLine.Brand,
                UnitPriceAmount = orderLine.UnitPrice.Amount,
                UnitPriceCurrency = orderLine.UnitPrice.Currency,
                Quantity = orderLine.Quantity,
                TotalAmount = orderLine.Total.Amount,
                TotalCurrency = orderLine.Total.Currency
            };
        }
    }
}
