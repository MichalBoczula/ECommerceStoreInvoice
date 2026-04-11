using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Invoices;

public sealed class InvoicePdfServiceTests
{
    [Fact]
    public void GetTemplatePath_ShouldReturnInvoiceTemplatePathUnderTemplatesDirectory()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceTemplate.html");

        // Act
        var templatePath = sut.GetTemplatePath();

        // Assert
        templatePath.ShouldBe(expectedPath);
    }

    [Fact]
    public void GetLineTemplatePath_ShouldReturnInvoiceLineTemplatePathUnderTemplatesDirectory()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceLineTemplate.html");

        // Act
        var lineTemplatePath = sut.GetLineTemplatePath();

        // Assert
        lineTemplatePath.ShouldBe(expectedPath);
    }

    [Fact]
    public void BuildInvoiceLines_WhenOrderHasLines_ShouldMapAllFieldsFromOrderLines()
    {
        // Arrange
        var orderLines = new List<OrderLine>
        {
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Gaming Mouse",
                "Logifast",
                new Money(99.99m, "USD"),
                2),
            new(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Mechanical Keyboard",
                "KeyLabs",
                new Money(150.00m, "EUR"),
                1)
        };

        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            orderLines,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            OrderStatus.Created,
            new Money(orderLines.Sum(x => x.Total.Amount), "USD"));

        var sut = new InvoicePdfService();

        // Act
        var result = sut.BuildInvoiceLines(order).ToList();

        // Assert
        result.Count.ShouldBe(orderLines.Count);

        result[0].ProductVersionId.ShouldBe(orderLines[0].ProductVersionId.ToString());
        result[0].Name.ShouldBe(orderLines[0].Name);
        result[0].Brand.ShouldBe(orderLines[0].Brand);
        result[0].Quantity.ShouldBe(orderLines[0].Quantity);
        result[0].UnitAmount.ShouldBe(orderLines[0].UnitPrice.Amount);
        result[0].TotalAmount.ShouldBe(orderLines[0].Total.Amount);
        result[0].Currency.ShouldBe(orderLines[0].UnitPrice.Currency);

        result[1].ProductVersionId.ShouldBe(orderLines[1].ProductVersionId.ToString());
        result[1].Name.ShouldBe(orderLines[1].Name);
        result[1].Brand.ShouldBe(orderLines[1].Brand);
        result[1].Quantity.ShouldBe(orderLines[1].Quantity);
        result[1].UnitAmount.ShouldBe(orderLines[1].UnitPrice.Amount);
        result[1].TotalAmount.ShouldBe(orderLines[1].Total.Amount);
        result[1].Currency.ShouldBe(orderLines[1].UnitPrice.Currency);
    }

    [Fact]
    public void BuildInvoiceLines_WhenOrderHasNoLines_ShouldReturnEmptyCollection()
    {
        // Arrange
        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            OrderStatus.Created,
            new Money(0m, "USD"));

        var sut = new InvoicePdfService();

        // Act
        var result = sut.BuildInvoiceLines(order);

        // Assert
        result.ShouldBeEmpty();
    }
}
