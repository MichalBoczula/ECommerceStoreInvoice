using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
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
                    x.ProductId,
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
                ProductId = shoppingCartLine.ProductId,
                Quantity = shoppingCartLine.Quantity
            };
        }

    }
}
