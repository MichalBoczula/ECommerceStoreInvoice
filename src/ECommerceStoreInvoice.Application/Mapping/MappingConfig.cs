using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
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

        public static Order MapToDomain(
            ShoppingCart shoppingCart,
            IReadOnlyCollection<ProductVersion> productVersions)
        {
            var orderLines = shoppingCart.Lines
                .Zip(productVersions, (shoppingCartLine, productVersion) => MapToDomain(shoppingCartLine, productVersion))
                .ToList();

            return new Order(
                shoppingCart.ClientId,
                orderLines);
        }

        public static ShoppingCartResponseDto MapToResponse(ShoppingCart shoppingCart)
        {
            return new ShoppingCartResponseDto
            {
                Id = shoppingCart.Id,
                ClientId = shoppingCart.ClientId,
                CreatedAt = shoppingCart.CreatedAt,
                UpdatedAt = shoppingCart.UpdatedAt,
                Lines = shoppingCart.Lines.Select(MapToResponse).ToList()
            };
        }

        public static OrderResponseDto MapToResponse(
            (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) orderWithProducts)
        {
            return MapToResponse(orderWithProducts.Order, orderWithProducts.ProductVersions);
        }

        public static OrderResponseDto MapToResponse(
            Order order,
            IReadOnlyCollection<ProductVersion> productVersions)
        {
            var productVersionDtos = productVersions.Select(MapToResponse).ToList();
            return MapToResponse(order, productVersionDtos);
        }

        public static OrderResponseDto MapToResponse(
            Order order,
            IReadOnlyCollection<ProductVersionResponseDto> productVersions)
        {
            var versionsDict = productVersions.ToDictionary(pv => pv.Id);

            var lineDtos = order.Lines.Select(orderLine =>
            {
                if (!versionsDict.TryGetValue(orderLine.ProductVersionId, out var version))
                {
                    throw new InvalidOperationException($"ProductVersion with id '{orderLine.ProductVersionId}' was not found for Order '{order.Id}'.");
                }

                return MapToResponse(orderLine, version);
            }).ToList();

            var totalAmount = lineDtos.Sum(l => l.LineTotalAmount);
            var totalCurrency = lineDtos.FirstOrDefault()?.ProductVersion.PriceCurrency ?? "USD";

            return new OrderResponseDto
            {
                Id = order.Id,
                ClientId = order.ClientId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Status = order.Status.ToString(),
                TotalAmount = totalAmount,
                TotalCurrency = totalCurrency,
                Lines = lineDtos
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
                request.Quantity);
        }

        private static OrderLine MapToDomain(ShoppingCartLine shoppingCartLine, ProductVersion productVersion)
        {
            return new OrderLine(
                productVersion.Id,
                shoppingCartLine.Quantity);
        }

        private static ShoppingCartLineResponseDto MapToResponse(ShoppingCartLine shoppingCartLine)
        {
            return new ShoppingCartLineResponseDto
            {
                ProductId = shoppingCartLine.ProductId,
                Quantity = shoppingCartLine.Quantity
            };
        }

        public static OrderLineResponseDto MapToResponse(
            OrderLine orderLine,
            ProductVersionResponseDto productVersion)
        {
            return new OrderLineResponseDto
            {
                ProductVersionId = orderLine.ProductVersionId,
                Quantity = orderLine.Quantity,
                ProductVersion = productVersion,
                LineTotalAmount = productVersion.PriceAmount * orderLine.Quantity
            };
        }
    }
}