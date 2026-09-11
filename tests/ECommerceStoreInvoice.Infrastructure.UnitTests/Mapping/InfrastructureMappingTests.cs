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
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products.Models;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Mapping;

public sealed class InfrastructureMappingTests
{
    [Fact]
    public void ClientDataVersionMapping_ShouldMapBothDirections()
    {
        var domain = ClientDataVersion.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jane Doe",
            new Address("00-001", "Warsaw", "Main", "10A", "2"),
            "123456789",
            "+48",
            "jane@example.com",
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        var document = ClientDataVersionMapping.MapToDocument(domain);

        document.ClientName.ShouldBe(domain.ClientName);
        document.PostalCode.ShouldBe(domain.Address.PostalCode);
        document.PhonePrefix.ShouldBe(domain.PhonePrefix);

        var mappedBack = ClientDataVersionMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.ClientId.ShouldBe(domain.ClientId);
        mappedBack.Address.Street.ShouldBe(domain.Address.Street);
        mappedBack.AddressEmail.ShouldBe(domain.AddressEmail);
    }

    [Fact]
    public void ProductVersionMapping_ShouldMapBothDirections()
    {
        var domain = ProductVersion.Rehydrate(
            Guid.NewGuid(),
            false,
            new DateTime(2026, 1, 5, 7, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            new Money(45.99m, "USD"),
            "Headphones",
            "Contoso");

        var document = ProductVersionMapping.MapToDocument(domain);

        document.PriceAmount.ShouldBe(45.99m);
        document.PriceCurrency.ShouldBe("USD");
        document.IsActive.ShouldBeFalse();

        var mappedBack = ProductVersionMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.ProductId.ShouldBe(domain.ProductId);
        mappedBack.Price.Amount.ShouldBe(domain.Price.Amount);
        mappedBack.DeactivatedAt.ShouldBe(domain.DeactivatedAt);
    }

    [Fact]
    public void InvoiceMapping_ShouldMapBothDirections()
    {
        var domain = Invoice.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://storage.example/invoices/42.pdf",
            new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc));

        var document = InvoiceMapping.MapToDocument(domain);

        document.OrderId.ShouldBe(domain.OrderId);
        document.ClientDataVersionId.ShouldBe(domain.ClientDataVersionId);

        var mappedBack = InvoiceMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.StorageUrl.ShouldBe(domain.StorageUrl);
        mappedBack.CreatedAt.ShouldBe(domain.CreatedAt);
    }

    [Fact]
    public void OrderMapping_ShouldMapBothDirectionsIncludingLines()
    {
        var line = new OrderLine(Guid.NewGuid(), 2);
        var domain = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [line],
            new DateTime(2026, 2, 2, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc),
            OrderStatus.Paid);

        var document = OrderMapping.MapToDocument(domain);

        document.Status.ShouldBe(OrderStatus.Paid);
        document.Lines.Count.ShouldBe(1);
        document.Lines.Single().ProductVersionId.ShouldBe(line.ProductVersionId);
        document.Lines.Single().Quantity.ShouldBe(2);

        var mappedBack = OrderMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.Status.ShouldBe(OrderStatus.Paid);
        mappedBack.Lines.Single().ProductVersionId.ShouldBe(line.ProductVersionId);
        mappedBack.Lines.Single().Quantity.ShouldBe(line.Quantity);
    }

    [Fact]
    public void OrderMapping_MapToDomainWithProducts_ShouldMapOrderAndProductVersions()
    {
        // arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var firstProductVersionId = Guid.NewGuid();
        var secondProductVersionId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 2, 3, 8, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 2, 3, 9, 0, 0, DateTimeKind.Utc);
        var firstProductCreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var secondProductCreatedAt = new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Utc);
        var secondProductDeactivatedAt = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        var document = new OrderWithProductsDocument
        {
            Id = orderId,
            ClientId = clientId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Status = OrderStatus.Paid,
            Lines =
            [
                new OrderLineDocument { ProductVersionId = firstProductVersionId, Quantity = 2 },
                new OrderLineDocument { ProductVersionId = secondProductVersionId, Quantity = 1 }
            ],
            ProductVersions =
            [
                new ProductVersionDocument
                {
                    Id = firstProductVersionId,
                    IsActive = true,
                    CreatedAt = firstProductCreatedAt,
                    DeactivatedAt = null,
                    ProductId = firstProductId,
                    PriceAmount = 1299.99m,
                    PriceCurrency = "USD",
                    Name = "Phone Pro",
                    Brand = "Contoso"
                },
                new ProductVersionDocument
                {
                    Id = secondProductVersionId,
                    IsActive = false,
                    CreatedAt = secondProductCreatedAt,
                    DeactivatedAt = secondProductDeactivatedAt,
                    ProductId = secondProductId,
                    PriceAmount = 49.50m,
                    PriceCurrency = "EUR",
                    Name = "Protective Case",
                    Brand = "Fabrikam"
                }
            ]
        };

        // act
        var (order, productVersions) = OrderMapping.MapToDomain(document);

        // assert
        order.Id.ShouldBe(orderId);
        order.ClientId.ShouldBe(clientId);
        order.CreatedAt.ShouldBe(createdAt);
        order.UpdatedAt.ShouldBe(updatedAt);
        order.Status.ShouldBe(OrderStatus.Paid);
        order.Lines.Count.ShouldBe(2);
        order.Lines.ShouldContain(line => line.ProductVersionId == firstProductVersionId && line.Quantity == 2);
        order.Lines.ShouldContain(line => line.ProductVersionId == secondProductVersionId && line.Quantity == 1);

        productVersions.Count.ShouldBe(2);
        var firstProductVersion = productVersions.Single(version => version.Id == firstProductVersionId);
        firstProductVersion.IsActive.ShouldBeTrue();
        firstProductVersion.CreatedAt.ShouldBe(firstProductCreatedAt);
        firstProductVersion.DeactivatedAt.ShouldBeNull();
        firstProductVersion.ProductId.ShouldBe(firstProductId);
        firstProductVersion.Price.Amount.ShouldBe(1299.99m);
        firstProductVersion.Price.Currency.ShouldBe("USD");
        firstProductVersion.Name.ShouldBe("Phone Pro");
        firstProductVersion.Brand.ShouldBe("Contoso");

        var secondProductVersion = productVersions.Single(version => version.Id == secondProductVersionId);
        secondProductVersion.IsActive.ShouldBeFalse();
        secondProductVersion.CreatedAt.ShouldBe(secondProductCreatedAt);
        secondProductVersion.DeactivatedAt.ShouldBe(secondProductDeactivatedAt);
        secondProductVersion.ProductId.ShouldBe(secondProductId);
        secondProductVersion.Price.Amount.ShouldBe(49.50m);
        secondProductVersion.Price.Currency.ShouldBe("EUR");
        secondProductVersion.Name.ShouldBe("Protective Case");
        secondProductVersion.Brand.ShouldBe("Fabrikam");
    }

    [Fact]
    public void OrderMapping_MapToDomainWithProducts_ShouldMapEmptyCollections()
    {
        // arrange
        var document = new OrderWithProductsDocument
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 2, 4, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = null,
            Status = OrderStatus.Created
        };

        // act
        var (order, productVersions) = OrderMapping.MapToDomain(document);

        // assert
        order.Lines.ShouldBeEmpty();
        order.UpdatedAt.ShouldBeNull();
        productVersions.ShouldBeEmpty();
    }

    [Fact]
    public void ShoppingCartMapping_ShouldMapBothDirectionsIncludingLines()
    {
        var line = new ShoppingCartLine(Guid.NewGuid(), 3);
        var domain = ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            [line]);

        var document = ShoppingCartMapping.MapToDocument(domain);

        document.Lines.Count.ShouldBe(1);
        var mappedBack = ShoppingCartMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.ClientId.ShouldBe(domain.ClientId);
    }

    [Fact]
    public void ShoppingCartMapping_MapLineToDocument_ShouldMapLineFields()
    {
        var line = new ShoppingCartLine(Guid.NewGuid(), 2);

        var document = ShoppingCartMapping.MapLineToDocument(line);

        document.ProductId.ShouldBe(line.ProductId);
    }

    [Fact]
    public void ProductVersionMapping_MapToSnapshot_ShouldMapFullyPopulatedDto()
    {
        // arrange
        var productId = Guid.NewGuid();

        var dto = new MobilePhoneDto
        {
            Id = productId,
            Name = "Galaxy S26",
            Brand = "Samsung",
            Price = new MoneyDto
            {
                Amount = 3500.75,
                Currency = "EUR"
            }
        };

        // act
        var snapshot = ProductVersionMapping.MapToSnapshot(dto);

        // assert
        snapshot.ProductId.ShouldBe(productId);
        snapshot.Name.ShouldBe("Galaxy S26");
        snapshot.Brand.ShouldBe("Samsung");
        snapshot.Price.Amount.ShouldBe(3500.75m);
        snapshot.Price.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void ProductVersionMapping_MapToSnapshot_ShouldHandleNullValuesGracefully()
    {
        // arrange
        var dto = new MobilePhoneDto
        {
            Id = null,
            Name = null,
            Brand = null,
            Price = null
        };

        // act
        var snapshot = ProductVersionMapping.MapToSnapshot(dto);

        // assert
        snapshot.ProductId.ShouldBe(Guid.Empty);
        snapshot.Name.ShouldBe(string.Empty);
        snapshot.Brand.ShouldBe(string.Empty);
        snapshot.Price.Amount.ShouldBe(0m);
        snapshot.Price.Currency.ShouldBe("USD");
    }
}
