using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Application.Mapping
{
    internal static class MappingConfig
    {
        public static ShoppingCart MapToDomain(UpdateShoppingCartRequestDto request)
        {
            var lines = request.Lines.Select(MapToDomain).ToList();

            return ShoppingCart.Rehydrate(
                request.Id,
                request.ClientId,
                request.CreatedAt,
                request.UpdatedAt,
                lines);
        }

        public static ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return new ShoppingCartResponseDto
            {
                Id = shoppingCart.Id,
                ClientId = shoppingCart.ClientId,
                CreatedAt = shoppingCart.CreatedAt,
                UpdatedAt = shoppingCart.UpdatedAt,
                TotalAmount = shoppingCart.Total.Amount,
                TotalCurrency = shoppingCart.Total.Currency,
                Lines = shoppingCart.Lines.Select(MapToResponse).ToList()
            };
        }

        private static ShoppingCartLine MapToDomain(ShoppingCartLineRequestDto request)
        {
            return new ShoppingCartLine(
                request.ProductVersionId,
                request.Name,
                request.Brand,
                new Money(request.UnitPriceAmount, request.UnitPriceCurrency),
                request.Quantity);
        }

        private static ShoppingCartLineResponseDto MapToResponse(ShoppingCartLine shoppingCartLine)
        {
            return new ShoppingCartLineResponseDto
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