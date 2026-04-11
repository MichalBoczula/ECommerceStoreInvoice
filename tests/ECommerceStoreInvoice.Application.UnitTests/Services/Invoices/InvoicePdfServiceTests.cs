using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Invoices;
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

    [Fact]
    public void ReplaceOrderLinesSection_WhenTemplateContainsTbody_ShouldReplaceBodyWithBuiltRows()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var lines = new List<InvoiceLineDto>
        {
            new()
            {
                ProductVersionId = "11111111-1111-1111-1111-111111111111",
                Name = "Gaming Mouse",
                Brand = "Logifast",
                Quantity = 2,
                UnitAmount = 99.99m,
                TotalAmount = 199.98m,
                Currency = "USD"
            }
        };

        var expectedRow = sut.BuildLineRow(lines[0]);
        var template = """
            <table>
                <tbody>
                    old row
                </tbody>
            </table>
            """;

        // Act
        var result = sut.ReplaceOrderLinesSection(template, lines);

        // Assert
        result.ShouldContain("<tbody>");
        result.ShouldContain(expectedRow);
        result.ShouldNotContain("old row");
    }

    [Fact]
    public void BuildLineRow_WhenLineContainsHtmlSensitiveCharacters_ShouldEscapeAndFormatValues()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var line = new InvoiceLineDto
        {
            ProductVersionId = "11111111-1111-1111-1111-111111111111",
            Name = "<Mouse & Keyboard>",
            Brand = "\"Brand\" & Co",
            Quantity = 3,
            UnitAmount = 12.5m,
            TotalAmount = 37.5m,
            Currency = "USD"
        };

        // Act
        var result = sut.BuildLineRow(line);

        // Assert
        result.ShouldContain("&lt;Mouse &amp; Keyboard&gt;");
        result.ShouldContain("&quot;Brand&quot; &amp; Co");
        result.ShouldContain("12.50 USD");
        result.ShouldContain("37.50 USD");
        result.ShouldContain(">3<");
        result.ShouldNotContain("{{Line.Name}}");
        result.ShouldNotContain("{{Line.Brand}}");
    }

    [Fact]
    public void ApplyOrderTokens_WhenTemplateContainsOrderTokens_ShouldReplaceAllOrderPlaceholders()
    {
        // Arrange
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var order = Order.Rehydrate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            [],
            createdAt,
            createdAt,
            OrderStatus.Created,
            new Money(0m, "USD"));

        var sut = new InvoicePdfService();
        var template = "Invoice {{InvoiceNumber}} {{Order.Id}} {{Order.CreatedAtUtc}} {{Order.Status}} {{Order.ClientId}}";

        // Act
        var result = sut.ApplyOrderTokens(template, order);

        // Assert
        result.ShouldContain("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        result.ShouldContain("2025-01-02 03:04:05Z");
        result.ShouldContain(OrderStatus.Created.ToString());
        result.ShouldContain("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        result.ShouldNotContain("{{InvoiceNumber}}");
        result.ShouldNotContain("{{Order.Id}}");
        result.ShouldNotContain("{{Order.CreatedAtUtc}}");
        result.ShouldNotContain("{{Order.Status}}");
        result.ShouldNotContain("{{Order.ClientId}}");
    }

    [Fact]
    public void ApplyTotalsTokens_WhenTemplateContainsTotalsTokens_ShouldReplaceAllTotalsPlaceholders()
    {
        // Arrange
        const decimal subtotal = 123.4m;
        const decimal tax = 28.382m;
        const decimal grandTotal = 151.782m;
        const string currency = "USD";

        var sut = new InvoicePdfService();
        var template = """
            Subtotal: {{Order.Total.Amount}} {{Order.Total.Currency}}
            Tax: {{Invoice.Tax.Amount}} {{Invoice.Tax.Currency}}
            Grand Total: {{Invoice.GrandTotal.Amount}} {{Invoice.GrandTotal.Currency}}
            """;

        // Act
        var result = sut.ApplyTotalsTokens(template, subtotal, tax, grandTotal, currency);

        // Assert
        result.ShouldContain("Subtotal: 123.40 USD");
        result.ShouldContain("Tax: 28.38 USD");
        result.ShouldContain("Grand Total: 151.78 USD");
        result.ShouldNotContain("{{Order.Total.Amount}}");
        result.ShouldNotContain("{{Order.Total.Currency}}");
        result.ShouldNotContain("{{Invoice.Tax.Amount}}");
        result.ShouldNotContain("{{Invoice.Tax.Currency}}");
        result.ShouldNotContain("{{Invoice.GrandTotal.Amount}}");
        result.ShouldNotContain("{{Invoice.GrandTotal.Currency}}");
    }

    [Fact]
    public void ApplyFinalTokens_WhenTemplateContainsFinalTokens_ShouldReplaceInvoiceAndSectionPlaceholders()
    {
        // Arrange
        var orderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var sut = new InvoicePdfService();
        var template = """
            Invoice: {{Invoice.Id}}
            Issue: {{Invoice.IssueDateUtc}}
            Generated: {{Invoice.GeneratedAtUtc}}
            {{#Order.Lines}}line{{/Order.Lines}}
            """;

        // Act
        var result = sut.ApplyFinalTokens(template, orderId);

        // Assert
        result.ShouldContain(orderId.ToString());
        result.ShouldMatch(@".*Issue: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z.*");
        result.ShouldMatch(@".*Generated: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z.*");
        result.ShouldContain("line");
        result.ShouldNotContain("{{Invoice.Id}}");
        result.ShouldNotContain("{{Invoice.IssueDateUtc}}");
        result.ShouldNotContain("{{Invoice.GeneratedAtUtc}}");
        result.ShouldNotContain("{{#Order.Lines}}");
        result.ShouldNotContain("{{/Order.Lines}}");
    }
}
