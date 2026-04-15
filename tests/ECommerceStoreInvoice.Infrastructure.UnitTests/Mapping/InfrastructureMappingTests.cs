using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.ClientDataVersions;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using ECommerceStoreInvoice.Infrastructure.Persistence.Orders;
using ECommerceStoreInvoice.Infrastructure.Persistence.ProductVersions;
using ECommerceStoreInvoice.Infrastructure.Persistence.ShoppingCarts;
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
        var line = new OrderLine(Guid.NewGuid(), "Monitor", "Fabrikam", new Money(400m, "USD"), 2);
        var domain = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [line],
            new DateTime(2026, 2, 2, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc),
            OrderStatus.Paid,
            new Money(800m, "USD"));

        var document = OrderMapping.MapToDocument(domain);

        document.Status.ShouldBe(OrderStatus.Paid);
        document.TotalAmount.ShouldBe(800m);
        document.Lines.Count.ShouldBe(1);
        document.Lines.Single().TotalAmount.ShouldBe(800m);

        var mappedBack = OrderMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.Status.ShouldBe(OrderStatus.Paid);
        mappedBack.Total.Currency.ShouldBe("USD");
        mappedBack.Lines.Single().ProductVersionId.ShouldBe(line.ProductVersionId);
    }

    [Fact]
    public void ShoppingCartMapping_ShouldMapBothDirectionsIncludingLines()
    {
        var line = new ShoppingCartLine(Guid.NewGuid(), "Keyboard", "Contoso", new Money(120m, "USD"), 3);
        var domain = ShoppingCart.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            [line]);

        var document = ShoppingCartMapping.MapToDocument(domain);

        document.Lines.Count.ShouldBe(1);
        document.Lines.Single().UnitPriceAmount.ShouldBe(120m);
        document.Lines.Single().TotalAmount.ShouldBe(360m);

        var mappedBack = ShoppingCartMapping.MapToDomain(document);

        mappedBack.Id.ShouldBe(domain.Id);
        mappedBack.ClientId.ShouldBe(domain.ClientId);
        mappedBack.Total.Amount.ShouldBe(360m);
        mappedBack.Lines.Single().Name.ShouldBe("Keyboard");
    }

    [Fact]
    public void ShoppingCartMapping_MapLineToDocument_ShouldMapLineFields()
    {
        var line = new ShoppingCartLine(Guid.NewGuid(), "Mouse", "Contoso", new Money(35m, "USD"), 2);

        var document = ShoppingCartMapping.MapLineToDocument(line);

        document.ProductId.ShouldBe(line.ProductId);
        document.UnitPriceCurrency.ShouldBe("USD");
        document.TotalAmount.ShouldBe(70m);
        document.TotalCurrency.ShouldBe("USD");
    }
}
