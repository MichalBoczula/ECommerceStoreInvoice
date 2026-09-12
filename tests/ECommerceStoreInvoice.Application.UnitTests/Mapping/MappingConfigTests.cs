using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
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

    [Fact]
    public void MapToDomain_Order_ShouldUseProductVersionIdsAndCartLineQuantities()
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

        var orderLine = result.Lines.Single();
        orderLine.ProductVersionId.ShouldBe(productVersion.Id);
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
    public void MapToResponse_OrderTuple_ShouldMapOrderAndProductVersions()
    {
        // Arrange
        var productVersion = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            true,
            new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            null,
            Guid.NewGuid(),
            new Money(25m, "EUR"),
            "Keyboard",
            "Contoso");

        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderLine(productVersion.Id, 3)],
            new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc),
            OrderStatus.Paid);

        (Order Order, IReadOnlyCollection<ProductVersion> ProductVersions) orderWithProducts =
            (order, [productVersion]);

        // Act
        var result = MappingConfig.MapToResponse(orderWithProducts);

        // Assert
        result.Id.ShouldBe(order.Id);
        result.ClientId.ShouldBe(order.ClientId);
        result.CreatedAt.ShouldBe(order.CreatedAt);
        result.UpdatedAt.ShouldBe(order.UpdatedAt);
        result.Status.ShouldBe(OrderStatus.Paid.ToString());
        result.TotalAmount.ShouldBe(75m);
        result.TotalCurrency.ShouldBe("EUR");

        var line = result.Lines.ShouldHaveSingleItem();
        line.ProductVersionId.ShouldBe(productVersion.Id);
        line.Quantity.ShouldBe(3);
        line.ProductVersion.Id.ShouldBe(productVersion.Id);
        line.ProductVersion.Name.ShouldBe("Keyboard");
    }

    [Fact]
    public void MapToResponse_OrderAndDomainProductVersions_ShouldMapOrderAndProductVersions()
    {
        // Arrange
        var productVersion = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            false,
            new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            new Money(12.50m, "PLN"),
            "Mouse",
            "Fabrikam");

        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderLine(productVersion.Id, 4)],
            new DateTime(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 4, 9, 0, 0, DateTimeKind.Utc),
            OrderStatus.Created);

        IReadOnlyCollection<ProductVersion> productVersions = [productVersion];

        // Act
        var result = MappingConfig.MapToResponse(order, productVersions);

        // Assert
        result.Id.ShouldBe(order.Id);
        result.ClientId.ShouldBe(order.ClientId);
        result.Status.ShouldBe(OrderStatus.Created.ToString());
        result.TotalAmount.ShouldBe(50m);
        result.TotalCurrency.ShouldBe("PLN");

        var line = result.Lines.ShouldHaveSingleItem();
        line.ProductVersionId.ShouldBe(productVersion.Id);
        line.Quantity.ShouldBe(4);
        line.LineTotalAmount.ShouldBe(50m);
        line.ProductVersion.Id.ShouldBe(productVersion.Id);
        line.ProductVersion.IsActive.ShouldBeFalse();
        line.ProductVersion.DeactivatedAt.ShouldBe(productVersion.DeactivatedAt);
        line.ProductVersion.PriceAmount.ShouldBe(12.50m);
        line.ProductVersion.PriceCurrency.ShouldBe("PLN");
        line.ProductVersion.Name.ShouldBe("Mouse");
        line.ProductVersion.Brand.ShouldBe("Fabrikam");
    }

    [Fact]
    public void MapToResponse_OrderInvoiceProductVersionAndClientDataVersion_ShouldMapAllExposedFields()
    {
        // Arrange
        var productVersionId = Guid.NewGuid();

        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderLine(productVersionId, 2)],
            new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc),
            OrderStatus.Paid);

        var invoice = Invoice.Rehydrate(
            Guid.NewGuid(),
            order.Id,
            Guid.NewGuid(),
            "https://storage.example/invoices/1.pdf",
            new DateTime(2026, 2, 3, 10, 0, 0, DateTimeKind.Utc));

        var productVersion = ProductVersion.Rehydrate(
            productVersionId,
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

        var productVersionResponse = MappingConfig.MapToResponse(productVersion);
        var clientDataVersionResponse = MappingConfig.MapToResponse(clientDataVersion);

        // Act
        var orderResponse = MappingConfig.MapToResponse(order, [productVersionResponse]);
        var invoiceResponse = MappingConfig.MapToResponse(invoice);

        // Assert
        orderResponse.Status.ShouldBe(OrderStatus.Paid.ToString());
        orderResponse.TotalAmount.ShouldBe(110m); // 55 * 2
        orderResponse.TotalCurrency.ShouldBe("USD");
        orderResponse.Lines.Count.ShouldBe(1);

        var line = orderResponse.Lines.Single();
        line.ProductVersionId.ShouldBe(productVersionId);
        line.Quantity.ShouldBe(2);
        line.LineTotalAmount.ShouldBe(110m);
        line.ProductVersion.Name.ShouldBe("Headphones");

        invoiceResponse.OrderId.ShouldBe(order.Id);
        invoiceResponse.StorageUrl.ShouldBe(invoice.StorageUrl);

        productVersionResponse.PriceAmount.ShouldBe(55m);
        productVersionResponse.Name.ShouldBe("Headphones");

        clientDataVersionResponse.ClientName.ShouldBe("John Doe");
        clientDataVersionResponse.PostalCode.ShouldBe("10-100");
        clientDataVersionResponse.PhonePrefix.ShouldBe("+49");
    }
}
