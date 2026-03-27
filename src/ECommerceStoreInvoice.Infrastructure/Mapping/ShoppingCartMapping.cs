using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;

namespace ECommerceStoreInvoice.Infrastructure.Mapping
{
    internal static class ShoppingCartMapping
    {
        internal static ShoppingCartDocument MapToDocument(ShoppingCart shoppingCart)
        {
            return new ShoppingCartDocument
            {
                Id = shoppingCart.Id,
                ClientId = shoppingCart.ClientId,
                CreatedAt = shoppingCart.CreatedAt,
                UpdatedAt = shoppingCart.UpdatedAt,
                Lines = shoppingCart.Lines.Select(MapLineToDocument).ToList()
            };
        }

        internal static ShoppingCart MapToDomain(ShoppingCartDocument shoppingCartDocument)
        {
            var lines = shoppingCartDocument.Lines.Select(x =>
                new ShoppingCartLine(
                    x.ProductVersionId,
                    x.Name,
                    x.Brand,
                    new Money(x.UnitPriceAmount, x.UnitPriceCurrency),
                    x.Quantity));

            return ShoppingCart.Rehydrate(
                shoppingCartDocument.Id,
                shoppingCartDocument.ClientId,
                shoppingCartDocument.CreatedAt,
                shoppingCartDocument.UpdatedAt,
                lines);
        }

        internal static ShoppingCartLineDocument MapLineToDocument(ShoppingCartLine shoppingCartLine)
        {
            return new ShoppingCartLineDocument
            {
                ProductVersionId = shoppingCartLine.ProductVersionId,
                Name = shoppingCartLine.Name,
                Brand = shoppingCartLine.Brand,
                UnitPriceAmount = shoppingCartLine.UnitPrice.Amount,
                UnitPriceCurrency = shoppingCartLine.UnitPrice.Currency,
                Quantity = shoppingCartLine.Quantity,
                TotalAmount = shoppingCartLine.Total.Amount,
                TotalCurrency = shoppingCartLine.Total.Currency
            };
        }

    }
}
