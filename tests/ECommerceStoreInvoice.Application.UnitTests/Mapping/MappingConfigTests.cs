using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Mapping;

public sealed class MappingConfigTests
{
    [Fact]
    public void MapToDomain_ShoppingCartLines_ShouldMapAllFields()
    {
        // Arrange
        var productId = Guid.NewGuid();
        IReadOnlyCollection<ShoppingCartLineRequestDto> requestLines =
        [
            new()
            {
                ProductId = productId,
                Quantity = 2
            }
        ];

        // Act
        var result = MappingConfig.MapToDomain(requestLines);

        // Assert
        result.Count.ShouldBe(1);
        var line = result.Single();
        line.ProductId.ShouldBe(productId);
        line.Quantity.ShouldBe(2);
    }

    [Fact]
    public void MapToDomain_ClientDataVersion_ShouldMapPrimitiveAndAddressFields()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var request = new CreateClientDataVersionRequestDto
        {
            ClientName = "Jane Doe",
            PostalCode = "00-001",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "10A",
            ApartmentNumber = "2",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "jane@example.com"
        };

        // Act
        var result = MappingConfig.MapToDomain(clientId, request);

        // Assert
        result.ClientId.ShouldBe(clientId);
        result.ClientName.ShouldBe(request.ClientName);
        result.Address.PostalCode.ShouldBe(request.PostalCode);
        result.Address.City.ShouldBe(request.City);
        result.Address.Street.ShouldBe(request.Street);
        result.Address.BuildingNumber.ShouldBe(request.BuildingNumber);
        result.Address.ApartmentNumber.ShouldBe(request.ApartmentNumber);
        result.PhoneNumber.ShouldBe(request.PhoneNumber);
        result.PhonePrefix.ShouldBe(request.PhonePrefix);
        result.AddressEmail.ShouldBe(request.AddressEmail);
    }

    [Fact(Skip = "Order require reimplementation")]
    public void MapToDomain_Order_ShouldUseProductVersionValuesAndCartLineQuantities()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cartLine = new ShoppingCartLine(productId, 2);

        var shoppingCart = ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            clientId,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            [cartLine]);

        var productVersion = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            true,
            DateTime.UtcNow.AddDays(-1),
            null,
            productId,
            new Money(1000m, "USD"),
            "Laptop",
            "Fabrikam");

        // Act
        var result = MappingConfig.MapToDomain(shoppingCart, [productVersion]);

        // Assert
        result.ClientId.ShouldBe(clientId);
        result.Lines.Count.ShouldBe(1);
        result.Total.Amount.ShouldBe(2000m);
        result.Total.Currency.ShouldBe("USD");

        var orderLine = result.Lines.Single();
        orderLine.ProductVersionId.ShouldBe(productVersion.Id);
        orderLine.Name.ShouldBe(productVersion.Name);
        orderLine.Brand.ShouldBe(productVersion.Brand);
        orderLine.UnitPrice.Amount.ShouldBe(productVersion.Price.Amount);
        orderLine.UnitPrice.Currency.ShouldBe(productVersion.Price.Currency);
        orderLine.Quantity.ShouldBe(cartLine.Quantity);
    }

    [Fact]
    public void MapToResponse_ShoppingCart_ShouldMapHeaderAndLineQuantities()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var line = new ShoppingCartLine(productId, 3);

        var shoppingCart = ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            clientId,
            new DateTime(2026, 1, 2, 10, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 3, 11, 45, 0, DateTimeKind.Utc),
            [line]);

        // Act
        var result = MappingConfig.MapToResponse(shoppingCart);

        // Assert
        result.Id.ShouldBe(shoppingCart.Id);
        result.ClientId.ShouldBe(clientId);
        result.CreatedAt.ShouldBe(shoppingCart.CreatedAt);
        result.UpdatedAt.ShouldBe(shoppingCart.UpdatedAt);
        result.Lines.Count.ShouldBe(1);
        result.Lines.Single().ProductId.ShouldBe(productId);
        result.Lines.Single().Quantity.ShouldBe(3);
    }

    [Fact]
    public void MapToResponse_OrderInvoiceProductVersionAndClientDataVersion_ShouldMapAllExposedFields()
    {
        // Arrange
        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderLine(Guid.NewGuid(), "Monitor", "Fabrikam", new Money(400m, "USD"), 2)],
            new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc),
            OrderStatus.Paid,
            new Money(800m, "USD"));

        var invoice = Invoice.Rehydrate(
            Guid.NewGuid(),
            order.Id,
            Guid.NewGuid(),
            "https://storage.example/invoices/1.pdf",
            new DateTime(2026, 2, 3, 10, 0, 0, DateTimeKind.Utc));

        var productVersion = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            true,
            new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            null,
            Guid.NewGuid(),
            new Money(55m, "USD"),
            "Headphones",
            "Contoso");

        var clientDataVersion = ClientDataVersion.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            new Address("10-100", "Berlin", "Tech Street", "9", "4"),
            "111222333",
            "+49",
            "john@example.com",
            new DateTime(2026, 2, 4, 11, 0, 0, DateTimeKind.Utc));

        // Act
        var orderResponse = MappingConfig.MapToResponse(order);
        var invoiceResponse = MappingConfig.MapToResponse(invoice);
        var productVersionResponse = MappingConfig.MapToResponse(productVersion);
        var clientDataVersionResponse = MappingConfig.MapToResponse(clientDataVersion);

        // Assert
        orderResponse.Status.ShouldBe(OrderStatus.Paid.ToString());
        orderResponse.TotalAmount.ShouldBe(800m);
        orderResponse.Lines.Single().TotalAmount.ShouldBe(800m);

        invoiceResponse.OrderId.ShouldBe(order.Id);
        invoiceResponse.StorageUrl.ShouldBe(invoice.StorageUrl);

        productVersionResponse.PriceAmount.ShouldBe(55m);
        productVersionResponse.Name.ShouldBe("Headphones");

        clientDataVersionResponse.ClientName.ShouldBe("John Doe");
        clientDataVersionResponse.PostalCode.ShouldBe("10-100");
        clientDataVersionResponse.PhonePrefix.ShouldBe("+49");
    }
}