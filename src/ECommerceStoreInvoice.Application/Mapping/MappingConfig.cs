using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;

namespace ECommerceStoreInvoice.Application.Mapping
{
    internal static class MappingConfig
    {
        public static IReadOnlyCollection<ShoppingCartLine> MapToDomain(
            IReadOnlyCollection<ShoppingCartLineRequestDto> requestLines)
        {
            return requestLines.Select(MapToDomain).ToList();
        }

        public static ClientDataVersion MapToDomain(Guid clientId, CreateClientDataVersionRequestDto request)
        {
            return new ClientDataVersion(
                clientId,
                request.ClientName,
                new Address(
                    request.PostalCode,
                    request.City,
                    request.Street,
                    request.BuildingNumber,
                    request.ApartmentNumber),
                request.PhoneNumber,
                request.PhonePrefix,
                request.AddressEmail);
        }

        public static Order MapToDomain(ShoppingCart shoppingCart)
        {
            return new Order(
                shoppingCart.ClientId,
                shoppingCart.Lines.Select(MapToDomain).ToList());
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

        public static OrderResponseDto MapToResponse(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                ClientId = order.ClientId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Status = order.Status.ToString(),
                TotalAmount = order.Total.Amount,
                TotalCurrency = order.Total.Currency,
                Lines = order.Lines.Select(MapToResponse).ToList()
            };
        }


        public static InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                OrderId = invoice.OrderId,
                ClietDataVersionId = invoice.ClientDataVersionId,
                StorageUrl = invoice.StorageUrl,
                CreatedAt = invoice.CreatedAt
            };
        }

        public static ProductVersionResponseDto MapToResponse(ProductVersion productVersion)
        {
            return new ProductVersionResponseDto
            {
                Id = productVersion.Id,
                IsActive = productVersion.IsActive,
                CreatedAt = productVersion.CreatedAt,
                DeactivatedAt = productVersion.DeactivatedAt,
                ProductId = productVersion.ProductId,
                PriceAmount = productVersion.Price.Amount,
                PriceCurrency = productVersion.Price.Currency,
                Name = productVersion.Name,
                Brand = productVersion.Brand
            };
        }

        public static ClientDataVersionResponseDto MapToResponse(ClientDataVersion clientDataVersion)
        {
            return new ClientDataVersionResponseDto
            {
                Id = clientDataVersion.Id,
                ClientId = clientDataVersion.ClientId,
                ClientName = clientDataVersion.ClientName,
                PostalCode = clientDataVersion.Address.PostalCode,
                City = clientDataVersion.Address.City,
                Street = clientDataVersion.Address.Street,
                BuildingNumber = clientDataVersion.Address.BuildingNumber,
                ApartmentNumber = clientDataVersion.Address.ApartmentNumber,
                PhoneNumber = clientDataVersion.PhoneNumber,
                PhonePrefix = clientDataVersion.PhonePrefix,
                AddressEmail = clientDataVersion.AddressEmail,
                CreatedAt = clientDataVersion.CreatedAt
            };
        }

        private static ShoppingCartLine MapToDomain(ShoppingCartLineRequestDto request)
        {
            return new ShoppingCartLine(
                request.ProductId,
                request.Name,
                request.Brand,
                new Money(request.UnitPriceAmount, request.UnitPriceCurrency),
                request.Quantity);
        }

        private static OrderLine MapToDomain(ShoppingCartLine shoppingCartLine)
        {
            return new OrderLine(
                shoppingCartLine.ProductId,
                shoppingCartLine.Name,
                shoppingCartLine.Brand,
                shoppingCartLine.UnitPrice,
                shoppingCartLine.Quantity);
        }

        private static ShoppingCartLineResponseDto MapToResponse(ShoppingCartLine shoppingCartLine)
        {
            return new ShoppingCartLineResponseDto
            {
                Name = shoppingCartLine.Name,
                ProductId = shoppingCartLine.ProductId,
                Brand = shoppingCartLine.Brand,
                UnitPriceAmount = shoppingCartLine.UnitPrice.Amount,
                UnitPriceCurrency = shoppingCartLine.UnitPrice.Currency,
                Quantity = shoppingCartLine.Quantity,
                TotalAmount = shoppingCartLine.Total.Amount,
                TotalCurrency = shoppingCartLine.Total.Currency
            };
        }

        private static OrderLineResponseDto MapToResponse(OrderLine orderLine)
        {
            return new OrderLineResponseDto
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
